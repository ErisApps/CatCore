using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using CatCore.Models.Twitch.PubSub;
using CatCore.Models.Twitch.PubSub.Requests;
using CatCore.Models.Twitch.PubSub.Responses.Polls;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal class TwitchPubSubServiceAgent : IAsyncDisposable
	{
		private const string TWITCH_PUBSUB_ENDPOINT = "wss://pubsub-edge.twitch.tv";
		private const string TWITCH_PUBSUB_PING_MESSAGE = @"{""type"": ""PING""}";

		private const int TWITCH_PUBSUB_PING_TIMER_DEFAULT_INTERVAL = 120 * 1000;
		private const int TWITCH_PUBSUB_PONG_TIMER_INTERVAL = 15 * 1000;

		private readonly ILogger _logger;
		private readonly IKittenWebSocketProvider _kittenWebSocketProvider;
		private readonly Random _random;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly string _channelId;

		private readonly Timer _pingTimer;
		private readonly Timer _pongTimer;

		private bool _hasPongBeenReceived;

		public TwitchPubSubServiceAgent(ILogger logger, Random random, ITwitchAuthService twitchAuthService, string channelId)
		{
			_logger = logger.ForContext<TwitchPubSubServiceAgent>();

			_random = random;
			_twitchAuthService = twitchAuthService;
			_channelId = channelId;

			_kittenWebSocketProvider = new KittenWebSocketProvider(logger); // manual resolution // TODO: Find a better way for this to ensure testability in the long run

			// According to the Twitch PubSub docs, we should send a ping message at least once every 5 minutes
			// => Sending one every 2 or 3 minutes sounds like a sane number.
			// We should also check whether we get a response within 10 seconds after the message has been send.
			// => Adding an additional 5 second buffer to the pong timer in case the connection is a bit slower though
			_pingTimer = new Timer {Interval = TWITCH_PUBSUB_PING_TIMER_DEFAULT_INTERVAL, AutoReset = false};
			_pingTimer.Elapsed += PingTimerOnElapsed;

			_pongTimer = new Timer {Interval = TWITCH_PUBSUB_PONG_TIMER_INTERVAL, AutoReset = false};
			_pongTimer.Elapsed += PongTimerOnElapsed;
		}

		private void PingTimerOnElapsed(object sender, ElapsedEventArgs e)
		{
			_pongTimer!.Stop();
			_pongTimer.Start();

			_hasPongBeenReceived = false;

			_kittenWebSocketProvider.SendMessage(TWITCH_PUBSUB_PING_MESSAGE);
		}

		private async void PongTimerOnElapsed(object sender, ElapsedEventArgs e)
		{
			if (!_hasPongBeenReceived)
			{
				await Start().ConfigureAwait(false);
			}
			else
			{
				_hasPongBeenReceived = false;

				_pingTimer.Interval = TWITCH_PUBSUB_PING_TIMER_DEFAULT_INTERVAL + _random.Next(30) * 1000;
				_pingTimer.Start();
			}
		}

		public async Task Start()
		{
			if (!_twitchAuthService.HasTokens || !_twitchAuthService.LoggedInUser.HasValue)
			{
				return;
			}

			if (!_twitchAuthService.TokenIsValid)
			{
				await _twitchAuthService.RefreshTokens().ConfigureAwait(false);
			}

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.ConnectHappened += ConnectHappenedHandler;

			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened += DisconnectHappenedHandler;

			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
			_kittenWebSocketProvider.MessageReceived += MessageReceivedHandler;

			await _kittenWebSocketProvider.Connect(TWITCH_PUBSUB_ENDPOINT).ConfigureAwait(false);
		}

		public async Task Stop()
		{
			await _kittenWebSocketProvider.Disconnect().ConfigureAwait(false);

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
		}

		public async ValueTask DisposeAsync()
		{
			_pingTimer?.Dispose();
			_pongTimer?.Dispose();

			await _kittenWebSocketProvider.Disconnect("Forced to go close").ConfigureAwait(false);
		}

		public void SendListenTopicPubSubMessage(string topic)
		{
			_kittenWebSocketProvider.SendMessage(JsonSerializer.Serialize(new ListenMessage(topic, new ListenMessage.ListenMessageData(_twitchAuthService.AccessToken!, topic))));
		}

		private void ConnectHappenedHandler()
		{
			_pingTimer!.Start();

			SendListenTopicPubSubMessage($"video-playback-by-id.{_channelId}");
			SendListenTopicPubSubMessage($"raid.{_channelId}");
			SendListenTopicPubSubMessage($"following.{_channelId}");

			SendListenTopicPubSubMessage($"channel-bits-events-v1.{_channelId}"); // Only on token channel
			SendListenTopicPubSubMessage($"channel-bits-events-v2.{_channelId}"); // Only on token channel
			SendListenTopicPubSubMessage($"channel-bits-badge-unlocks.{_channelId}"); // Only on token channel
			SendListenTopicPubSubMessage($"chat_moderator_actions.{_twitchAuthService.LoggedInUser!.Value.UserId}.{_channelId}");

			SendListenTopicPubSubMessage($"community-points-channel-v1.{_channelId}");

			SendListenTopicPubSubMessage($"polls.{_channelId}");
			SendListenTopicPubSubMessage($"predictions-channel-v1.{_channelId}");
		}

		private void DisconnectHappenedHandler()
		{
			_pingTimer.Stop();
			_pongTimer.Stop();
		}

		// ReSharper disable once CognitiveComplexity
		// ReSharper disable once CyclomaticComplexity
		private void MessageReceivedHandler(string receivedMessage)
		{
#if DEBUG
			var stopWatch = new Stopwatch();
			stopWatch.Start();
#endif

			try
			{
				var jsonDocument = JsonDocument.Parse(receivedMessage);
				var rootElement = jsonDocument.RootElement;

				var type = rootElement.GetProperty("type").GetString();

				_logger.Debug("New PubSub message of type: {Type}", type);

				switch (type)
				{
					case "RESPONSE":
						var error = rootElement.GetProperty("error").GetString()!;
						var nonce = rootElement.GetProperty("nonce").GetString()!;

						// TODO: handle topic registrations

						break;
					case "PONG":
						_hasPongBeenReceived = true;

						break;
					case "RECONNECT":
						Start().ConfigureAwait(false);

						break;
					case "MESSAGE":
						var data = rootElement.GetProperty("data");

						var topicSpan = data.GetProperty("topic").GetString()!.AsSpan();
						var firstDotSeparator = topicSpan.IndexOf('.');
						var topic = topicSpan.Slice(0, firstDotSeparator).ToString();
						var message = data.GetProperty("message").GetString()!;

						switch (topic)
						{
							case "video-playback-by-id":
							{
								var videoFeedbackDocument = JsonDocument.Parse(message).RootElement;
								var internalType = videoFeedbackDocument.GetProperty("type").GetString();
								var serverTime = videoFeedbackDocument.GetProperty("server_time").GetRawText();
								switch (internalType)
								{
									case "viewcount":
										var viewers = videoFeedbackDocument.GetProperty("viewers").GetUInt32();

										_logger.Debug("Main event type: {MainType} - Internal event type: {SubType} - Viewers: {Viewers}", topic, internalType, viewers);

										break;
									case "stream-up":
										var playDelay = videoFeedbackDocument.GetProperty("play_delay").GetInt32();

										_logger.Debug("Main event type: {MainType} - Internal event type: {SubType} - Play delay: {PlayDelay}", topic, internalType, playDelay);

										break;
									case "stream-down":
										_logger.Debug("Main event type: {MainType} - Internal event type: {SubType}", topic, internalType);

										break;
									case "commercial":
										var length = videoFeedbackDocument.GetProperty("length").GetInt32();

										_logger.Debug("Main event type: {MainType} - Internal event type: {SubType} - Length: {Length}", topic, internalType, length);

										break;
								}

								break;
							}
							case "following":
								var followingDocument = JsonDocument.Parse(message).RootElement;
								var displayName = followingDocument.GetProperty("display_name").GetString()!;
								var username = followingDocument.GetProperty("username").GetString()!;
								var userId = followingDocument.GetProperty("user_id").GetString()!;

								_logger.Debug("Main event type: {MainType} - {DisplayName} now follows channel {TargetChannelId}", topic, displayName, _channelId);

								break;
							case "community-points-channel-v1":
							{
								var communityPointsChannelDocument = JsonDocument.Parse(message).RootElement;
								var internalType = communityPointsChannelDocument.GetProperty("type").GetString();
								switch (internalType)
								{
									case "reward-redeemed":
										// {"type":"MESSAGE","data":{"topic":"community-points-channel-v1.47320688","message":"{\"type\":\"reward-redeemed\",\"data\":{\"timestamp\":\"2021-07-17T00:00:22.240109214Z\",\"redemption\":{\"id\":\"34341b1e-7f41-4c07-a3e8-eebdd8b61d08\",\"user\":{\"id\":\"214088964\",\"login\":\"soggynan_\",\"display_name\":\"Soggynan_\"},\"channel_id\":\"47320688\",\"redeemed_at\":\"2021-07-17T00:00:22.240109214Z\",\"reward\":{\"id\":\"7ede4f2f-37d4-4eed-9b09-2373907ed077\",\"channel_id\":\"47320688\",\"title\":\"Get Timed out\",\"prompt\":\"Mod will time you out if you redeem that\",\"cost\":249,\"is_user_input_required\":false,\"is_sub_only\":false,\"image\":null,\"default_image\":{\"url_1x\":\"https://static-cdn.jtvnw.net/custom-reward-images/default-1.png\",\"url_2x\":\"https://static-cdn.jtvnw.net/custom-reward-images/default-2.png\",\"url_4x\":\"https://static-cdn.jtvnw.net/custom-reward-images/default-4.png\"},\"background_color\":\"#000000\",\"is_enabled\":true,\"is_paused\":false,\"is_in_stock\":false,\"max_per_stream\":{\"is_enabled\":false,\"max_per_stream\":0},\"should_redemptions_skip_request_queue\":false,\"template_id\":null,\"updated_for_indicator_at\":\"2021-01-02T22:26:35.265101751Z\",\"max_per_user_per_stream\":{\"is_enabled\":false,\"max_per_user_per_stream\":0},\"global_cooldown\":{\"is_enabled\":true,\"global_cooldown_seconds\":60},\"redemptions_redeemed_current_stream\":null,\"cooldown_expires_at\":\"2021-07-17T00:01:22Z\"},\"status\":\"UNFULFILLED\",\"cursor\":\"MzQzNDFiMWUtN2Y0MS00YzA3LWEzZTgtZWViZGQ4YjYxZDA4X18yMDIxLTA3LTE3VDAwOjAwOjIyLjI0MDEwOTIxNFo=\"}}}"}}

										break;
									case "custom-reward-created":
										break;
									case "custom-reward-updated":
										// {"type":"custom-reward-updated","data":{"timestamp":"2021-07-17T00:00:22.240109214Z","updated_reward":{"id":"7ede4f2f-37d4-4eed-9b09-2373907ed077","channel_id":"47320688","title":"Get Timed out","prompt":"Mod will time you out if you redeem that","cost":249,"is_user_input_required":false,"is_sub_only":false,"image":null,"default_image":{"url_1x":"https://static-cdn.jtvnw.net/custom-reward-images/default-1.png","url_2x":"https://static-cdn.jtvnw.net/custom-reward-images/default-2.png","url_4x":"https://static-cdn.jtvnw.net/custom-reward-images/default-4.png"},"background_color":"#000000","is_enabled":true,"is_paused":false,"is_in_stock":false,"max_per_stream":{"is_enabled":false,"max_per_stream":0},"should_redemptions_skip_request_queue":false,"template_id":null,"updated_for_indicator_at":"2021-01-02T22:26:35.265101751Z","max_per_user_per_stream":{"is_enabled":false,"max_per_user_per_stream":0},"global_cooldown":{"is_enabled":true,"global_cooldown_seconds":60},"redemptions_redeemed_current_stream":null,"cooldown_expires_at":"2021-07-17T00:01:22Z"}}}

										break;
									case "custom-reward-deleted":
										break;
									default:
									case "redemption-status-update": // Maybe... just maybe...
									case "update-redemption-statuses-progress":
									case "update-redemption-statuses-finished":
										// NOP
										break;
								}

								break;
							}
							case "raid":
							{
								var raidDocument = JsonDocument.Parse(message).RootElement;
								var internalType = raidDocument.GetProperty("type").GetString();
								switch (internalType)
								{
									case "raid_update_v2":
										// {"type":"MESSAGE","data":{"topic":"raid.47320688","message":"{\"type\":\"raid_update_v2\",\"raid\":{\"id\":\"8f1ac7c1-3f80-4fe7-9e34-b54111603f6b\",\"creator_id\":\"47320688\",\"source_id\":\"47320688\",\"target_id\":\"121475892\",\"target_login\":\"calvinisacat\",\"target_display_name\":\"Calvinisacat\",\"target_profile_image\":\"https://static-cdn.jtvnw.net/jtv_user_pictures/b1c8b3d0-8732-416f-8ef8-7e4a4721a97c-profile_image-70x70.png\",\"transition_jitter_seconds\":0,\"force_raid_now_seconds\":90,\"viewer_count\":55}}"}}

										break;
									case "raid_go_v2":
										// {"type":"MESSAGE","data":{"topic":"raid.47320688","message":"{\"type\":\"raid_go_v2\",\"raid\":{\"id\":\"8f1ac7c1-3f80-4fe7-9e34-b54111603f6b\",\"creator_id\":\"47320688\",\"source_id\":\"47320688\",\"target_id\":\"121475892\",\"target_login\":\"calvinisacat\",\"target_display_name\":\"Calvinisacat\",\"target_profile_image\":\"https://static-cdn.jtvnw.net/jtv_user_pictures/b1c8b3d0-8732-416f-8ef8-7e4a4721a97c-profile_image-70x70.png\",\"transition_jitter_seconds\":0,\"force_raid_now_seconds\":90,\"viewer_count\":55}}"}}

										break;
								}

								break;
							}
							case "chat_moderator_actions":
								// JsonDocument.Parse(message).RootElement;
								break;
							case "channel-cheer-events-public-v1":

								break;
							case "polls":
							{
								var channelPredictionsDocument = JsonDocument.Parse(message).RootElement;
								var internalType = channelPredictionsDocument.GetProperty("type").GetString();
								var pollData = JsonSerializer.Deserialize<PollData>(channelPredictionsDocument.GetProperty("data").GetProperty("poll").GetRawText());
								switch (internalType)
								{
									case "POLL_CREATE":
									case "POLL_UPDATE":
									case "POLL_COMPLETE":
									case "POLL_ARCHIVE":
									case "POLL_TERMINATE":
										// TODO: Expose

										break;
									default:
										throw new NotSupportedException($"PubSub message of type {internalType} is currently not supported");
								}

								break;
							}
							case "predictions-channel-v1":
							{
								var channelPredictionsDocument = JsonDocument.Parse(message).RootElement;
								var internalType = channelPredictionsDocument.GetProperty("type").GetString();
								switch (internalType)
								{
									case "event-created":
										// {"type":"MESSAGE","data":{"topic":"predictions-channel-v1.47320688","message":"{\"type\":\"event-created\",\"data\":{\"timestamp\":\"2021-07-17T00:15:41.944161107Z\",\"event\":{\"id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"channel_id\":\"47320688\",\"created_at\":\"2021-07-17T00:15:41.928762325Z\",\"created_by\":{\"type\":\"USER\",\"user_id\":\"38834603\",\"user_display_name\":\"Kinsi55\",\"extension_client_id\":null},\"ended_at\":null,\"ended_by\":null,\"locked_at\":null,\"locked_by\":null,\"outcomes\":[{\"id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"color\":\"BLUE\",\"title\":\"Heads\",\"total_points\":0,\"total_users\":0,\"top_predictors\":[],\"badge\":{\"version\":\"blue-1\",\"set_id\":\"predictions\"}},{\"id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"color\":\"PINK\",\"title\":\"Tails\",\"total_points\":0,\"total_users\":0,\"top_predictors\":[],\"badge\":{\"version\":\"pink-2\",\"set_id\":\"predictions\"}}],\"prediction_window_seconds\":60,\"status\":\"ACTIVE\",\"title\":\"Coinflip\",\"winning_outcome_id\":null}}}"}}

										break;
									case "event-updated":
										// {"type":"MESSAGE","data":{"topic":"predictions-channel-v1.47320688","message":"{\"type\":\"event-updated\",\"data\":{\"timestamp\":\"2021-07-17T00:16:28.22094431Z\",\"event\":{\"id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"channel_id\":\"47320688\",\"created_at\":\"2021-07-17T00:15:41.928762325Z\",\"created_by\":{\"type\":\"USER\",\"user_id\":\"38834603\",\"user_display_name\":\"Kinsi55\",\"extension_client_id\":null},\"ended_at\":null,\"ended_by\":null,\"locked_at\":null,\"locked_by\":null,\"outcomes\":[{\"id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"color\":\"BLUE\",\"title\":\"Heads\",\"total_points\":2200,\"total_users\":4,\"top_predictors\":[{\"id\":\"c4631e9c64b7dfadacd834c57e96720d66bb882819da66d19d794a5a8f102172\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":2000,\"predicted_at\":\"2021-07-17T00:15:49.221355344Z\",\"updated_at\":\"2021-07-17T00:16:02.319039722Z\",\"user_id\":\"67232035\",\"result\":null,\"user_display_name\":\"DzRamen\"},{\"id\":\"54942c06ca4386f347d79877496881893da79428ed948ab98c8bfba148bd4400\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":130,\"predicted_at\":\"2021-07-17T00:16:03.579298023Z\",\"updated_at\":\"2021-07-17T00:16:14.626931781Z\",\"user_id\":\"461724447\",\"result\":null,\"user_display_name\":\"killerdoggy\"},{\"id\":\"84227c4a531fad7dcf8f23cb4708009d672d67a1544fc92b796a61f0aa2cacda\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":40,\"predicted_at\":\"2021-07-17T00:16:07.200220708Z\",\"updated_at\":\"2021-07-17T00:16:12.674881438Z\",\"user_id\":\"405499635\",\"result\":null,\"user_display_name\":\"RealEris\"},{\"id\":\"bc1956bebe7a3a4965a4ce96eddd07e9403b0f974f60e52f23c3d5255839850a\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b�~X-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":30,\"predicted_at\":\"2021-07-17T00:16:16.784452918Z\",\"updated_at\":\"2021-07-17T00:16:25.625576238Z\",\"user_id\":\"214088964\",\"result\":null,\"user_display_name\":\"Soggynan_\"}],\"badge\":{\"version\":\"blue-1\",\"set_id\":\"predictions\"}},{\"id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"color\":\"PINK\",\"title\":\"Tails\",\"total_points\":1830,\"total_users\":3,\"top_predictors\":[{\"id\":\"2c50fe074f01ff8fb5523255fbddffe43da759061c9b7a49112818a68079cecd\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"channel_id\":\"47320688\",\"points\":900,\"predicted_at\":\"2021-07-17T00:15:56.390188355Z\",\"updated_at\":\"2021-07-17T00:15:56.390188355Z\",\"user_id\":\"466366967\",\"result\":null,\"user_display_name\":\"Specture7\"},{\"id\":\"74a745867449af0fa92bfe4cbe0eed8252fe00fe85b9898e35b1e81044b0373f\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"channel_id\":\"47320688\",\"points\":510,\"predicted_at\":\"2021-07-17T00:16:05.631216824Z\",\"updated_at\":\"2021-07-17T00:16:11.395357211Z\",\"user_id\":\"170602007\",\"result\":null,\"user_display_name\":\"Ganuzz\"},{\"id\":\"9e62ba9bc4c5dc3931c242d5997ec4c5bbf6207dfb7059c83da66b5c166dd743\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"channel_id\":\"47320688\",\"points\":420,\"predicted_at\":\"2021-07-17T00:15:58.543177179Z\",\"updated_at\":\"2021-07-17T00:16:27.091757607Z\",\"user_id\":\"529849751\",\"result\":null,\"user_display_name\":\"OmegeyTheBot420\"}],\"badge\":{\"version\":\"pink-2\",\"set_id\":\"predictions\"}}],\"prediction_window_seconds\":60,\"status\":\"ACTIVE\",\"title\":\"Coinflip\",\"winning_outcome_id\":null}}}"}}
										// {"type":"MESSAGE","data":{"topic":"predictions-channel-v1.47320688","message":"{\"type\":\"event-updated\",\"data\":{\"timestamp\":\"2021-07-17T00:16:36.858457244Z\",\"event\":{\"id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"channel_id\":\"47320688\",\"created_at\":\"2021-07-17T00:15:41.928762325Z\",\"created_by\":{\"type\":\"USER\",\"user_id\":\"38834603\",\"user_display_name\":\"Kinsi55\",\"extension_client_id\":null},\"ended_at\":null,\"ended_by\":null,\"locked_at\":null,\"locked_by\":null,\"outcomes\":[{\"id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"color\":\"BLUE\",\"title\":\"Heads\",\"total_points\":3200,\"total_users\":4,\"top_predictors\":[{\"id\":\"c4631e9c64b7dfadacd834c57e96720d66bb882819da66d19d794a5a8f102172\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":3000,\"predicted_at\":\"2021-07-17T00:15:49.221355344Z\",\"updated_at\":\"2021-07-17T00:16:35.737737185Z\",\"user_id\":\"67232035\",\"result\":null,\"user_display_name\":\"DzRamen\"},{\"id\":\"54942c06ca4386f347d79877496881893da79428ed948ab98c8bfba148bd4400\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":130,\"predicted_at\":\"2021-07-17T00:16:03.579298023Z\",\"updated_at\":\"2021-07-17T00:16:14.626931781Z\",\"user_id\":\"461724447\",\"result\":null,\"user_display_name\":\"killerdoggy\"},{\"id\":\"84227c4a531fad7dcf8f23cb4708009d672d67a1544fc92b796a61f0aa2cacda\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":40,\"predicted_at\":\"2021-07-17T00:16:07.200220708Z\",\"updated_at\":\"2021-07-17T00:16:12.674881438Z\",\"user_id\":\"405499635\",\"result\":null,\"user_display_name\":\"RealEris\"},{\"id\":\"bc1956bebe7a3a4965a4ce96eddd07e9403b0f974f60e52f23c3d5255839850a\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8�~Yb-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":30,\"predicted_at\":\"2021-07-17T00:16:16.784452918Z\",\"updated_at\":\"2021-07-17T00:16:25.625576238Z\",\"user_id\":\"214088964\",\"result\":null,\"user_display_name\":\"Soggynan_\"}],\"badge\":{\"version\":\"blue-1\",\"set_id\":\"predictions\"}},{\"id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"color\":\"PINK\",\"title\":\"Tails\",\"total_points\":1830,\"total_users\":3,\"top_predictors\":[{\"id\":\"2c50fe074f01ff8fb5523255fbddffe43da759061c9b7a49112818a68079cecd\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"channel_id\":\"47320688\",\"points\":900,\"predicted_at\":\"2021-07-17T00:15:56.390188355Z\",\"updated_at\":\"2021-07-17T00:15:56.390188355Z\",\"user_id\":\"466366967\",\"result\":null,\"user_display_name\":\"Specture7\"},{\"id\":\"74a745867449af0fa92bfe4cbe0eed8252fe00fe85b9898e35b1e81044b0373f\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"channel_id\":\"47320688\",\"points\":510,\"predicted_at\":\"2021-07-17T00:16:05.631216824Z\",\"updated_at\":\"2021-07-17T00:16:11.395357211Z\",\"user_id\":\"170602007\",\"result\":null,\"user_display_name\":\"Ganuzz\"},{\"id\":\"9e62ba9bc4c5dc3931c242d5997ec4c5bbf6207dfb7059c83da66b5c166dd743\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"channel_id\":\"47320688\",\"points\":420,\"predicted_at\":\"2021-07-17T00:15:58.543177179Z\",\"updated_at\":\"2021-07-17T00:16:27.091757607Z\",\"user_id\":\"529849751\",\"result\":null,\"user_display_name\":\"OmegeyTheBot420\"}],\"badge\":{\"version\":\"pink-2\",\"set_id\":\"predictions\"}}],\"prediction_window_seconds\":60,\"status\":\"ACTIVE\",\"title\":\"Coinflip\",\"winning_outcome_id\":null}}}"}}
										// {"type":"MESSAGE","data":{"topic":"predictions-channel-v1.47320688","message":"{\"type\":\"event-updated\",\"data\":{\"timestamp\":\"2021-07-17T00:16:41.07731336Z\",\"event\":{\"id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"channel_id\":\"47320688\",\"created_at\":\"2021-07-17T00:15:41.928762325Z\",\"created_by\":{\"type\":\"USER\",\"user_id\":\"38834603\",\"user_display_name\":\"Kinsi55\",\"extension_client_id\":null},\"ended_at\":null,\"ended_by\":null,\"locked_at\":\"2021-07-17T00:16:41.067124146Z\",\"locked_by\":null,\"outcomes\":[{\"id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"color\":\"BLUE\",\"title\":\"Heads\",\"total_points\":3200,\"total_users\":4,\"top_predictors\":[{\"id\":\"c4631e9c64b7dfadacd834c57e96720d66bb882819da66d19d794a5a8f102172\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":3000,\"predicted_at\":\"2021-07-17T00:15:49.221355344Z\",\"updated_at\":\"2021-07-17T00:16:35.737737185Z\",\"user_id\":\"67232035\",\"result\":null,\"user_display_name\":\"DzRamen\"},{\"id\":\"54942c06ca4386f347d79877496881893da79428ed948ab98c8bfba148bd4400\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":130,\"predicted_at\":\"2021-07-17T00:16:03.579298023Z\",\"updated_at\":\"2021-07-17T00:16:14.626931781Z\",\"user_id\":\"461724447\",\"result\":null,\"user_display_name\":\"killerdoggy\"},{\"id\":\"84227c4a531fad7dcf8f23cb4708009d672d67a1544fc92b796a61f0aa2cacda\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":40,\"predicted_at\":\"2021-07-17T00:16:07.200220708Z\",\"updated_at\":\"2021-07-17T00:16:12.674881438Z\",\"user_id\":\"405499635\",\"result\":null,\"user_display_name\":\"RealEris\"},{\"id\":\"bc1956bebe7a3a4965a4ce96eddd07e9403b0f974f60e52f23c3d5255839850a\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edf�~vc2\",\"outcome_id\":\"1105fd8b-ceca-4956-8b49-88929dd1ca5d\",\"channel_id\":\"47320688\",\"points\":30,\"predicted_at\":\"2021-07-17T00:16:16.784452918Z\",\"updated_at\":\"2021-07-17T00:16:25.625576238Z\",\"user_id\":\"214088964\",\"result\":null,\"user_display_name\":\"Soggynan_\"}],\"badge\":{\"version\":\"blue-1\",\"set_id\":\"predictions\"}},{\"id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"color\":\"PINK\",\"title\":\"Tails\",\"total_points\":1830,\"total_users\":3,\"top_predictors\":[{\"id\":\"2c50fe074f01ff8fb5523255fbddffe43da759061c9b7a49112818a68079cecd\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"channel_id\":\"47320688\",\"points\":900,\"predicted_at\":\"2021-07-17T00:15:56.390188355Z\",\"updated_at\":\"2021-07-17T00:15:56.390188355Z\",\"user_id\":\"466366967\",\"result\":null,\"user_display_name\":\"Specture7\"},{\"id\":\"74a745867449af0fa92bfe4cbe0eed8252fe00fe85b9898e35b1e81044b0373f\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"channel_id\":\"47320688\",\"points\":510,\"predicted_at\":\"2021-07-17T00:16:05.631216824Z\",\"updated_at\":\"2021-07-17T00:16:11.395357211Z\",\"user_id\":\"170602007\",\"result\":null,\"user_display_name\":\"Ganuzz\"},{\"id\":\"9e62ba9bc4c5dc3931c242d5997ec4c5bbf6207dfb7059c83da66b5c166dd743\",\"event_id\":\"06f1f5b5-e363-4add-ba0b-b27d3c7edfc2\",\"outcome_id\":\"aa30085a-8585-4ecb-a7c7-12b1824a8b2b\",\"channel_id\":\"47320688\",\"points\":420,\"predicted_at\":\"2021-07-17T00:15:58.543177179Z\",\"updated_at\":\"2021-07-17T00:16:27.091757607Z\",\"user_id\":\"529849751\",\"result\":null,\"user_display_name\":\"OmegeyTheBot420\"}],\"badge\":{\"version\":\"pink-2\",\"set_id\":\"predictions\"}}],\"prediction_window_seconds\":60,\"status\":\"LOCKED\",\"title\":\"Coinflip\",\"winning_outcome_id\":null}}}"}}

										break;
								}

								break;
							}
							default:
								break;
						}

						break;
					default:
						_logger.Warning("Unhandled PubSub message type {Type}", type);
						break;
				}
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to parse a PubSub message");
			}

#if DEBUG
			stopWatch.Stop();
			_logger.Information("Handling of PubSub message took {ElapsedTime} ticks", stopWatch.ElapsedTicks);
#endif
		}
	}
}