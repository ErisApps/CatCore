using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using CatCore.Models.Twitch.PubSub;
using CatCore.Models.Twitch.PubSub.Requests;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;
using Websocket.Client;
using Websocket.Client.Models;
using ResponseMessage = Websocket.Client.ResponseMessage;

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

			_kittenWebSocketProvider = new KittenWebSocketProvider(); // manual resolution // TODO: Find a better way for this to ensure testability in the long run

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
			if (!_twitchAuthService.HasTokens)
			{
				return;
			}

			if (!_twitchAuthService.TokenIsValid)
			{
				await _twitchAuthService.RefreshTokens().ConfigureAwait(false);
			}

			_kittenWebSocketProvider.ReconnectHappened -= ReconnectHappenedHandler;
			_kittenWebSocketProvider.ReconnectHappened += ReconnectHappenedHandler;

			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened += DisconnectHappenedHandler;

			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
			_kittenWebSocketProvider.MessageReceived += MessageReceivedHandler;

			await _kittenWebSocketProvider.Connect(TWITCH_PUBSUB_ENDPOINT).ConfigureAwait(false);
		}

		public async Task Stop()
		{
			await _kittenWebSocketProvider.Disconnect().ConfigureAwait(false);

			_kittenWebSocketProvider.ReconnectHappened -= ReconnectHappenedHandler;
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

		protected void ReconnectHappenedHandler(ReconnectionInfo info)
		{
			_pingTimer!.Start();

			SendListenTopicPubSubMessage($"video-playback-by-id.{_channelId}");
			SendListenTopicPubSubMessage($"following.{_channelId}");

			SendListenTopicPubSubMessage($"channel-bits-events-v1.{_channelId}"); // Only on token channel
			SendListenTopicPubSubMessage($"channel-bits-events-v2.{_channelId}"); // Only on token channel
			SendListenTopicPubSubMessage($"channel-bits-badge-unlocks.{_channelId}"); // Only on token channel
			SendListenTopicPubSubMessage($"chat_moderator_actions.{_twitchAuthService.LoggedInUser!.Value.UserId}.{_channelId}");

			SendListenTopicPubSubMessage($"community-points-channel-v1.{_channelId}");
		}

		protected void DisconnectHappenedHandler(DisconnectionInfo info)
		{
			_pingTimer.Stop();
			_pongTimer.Stop();
		}

		protected void MessageReceivedHandler(ResponseMessage response)
		{
			try
			{
				var jsonDocument = JsonDocument.Parse(response.Text);
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
							case "following":
								var followingDocument = JsonDocument.Parse(message).RootElement;
								var displayName = followingDocument.GetProperty("display_name").GetString()!;
								var username = followingDocument.GetProperty("username").GetString()!;
								var userId = followingDocument.GetProperty("user_id").GetString()!;

								_logger.Debug("Main event type: {MainType} - {DisplayName} now follows channel {TargetChannelId}", topic, displayName, _channelId);

								break;
							case "community-points-channel-v1":
								var communityPointsChannelDocument = JsonDocument.Parse(message).RootElement;
								internalType = communityPointsChannelDocument.GetProperty("type").GetString();
								switch (internalType)
								{
									case "reward-redeemed":
										break;
									case "custom-reward-created":
										break;
									case "custom-reward-updated":
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
							case "raid":
								// JsonDocument.Parse(message).RootElement;
								break;
							case "chat_moderator_actions":
								// JsonDocument.Parse(message).RootElement;
								break;
							case "channel-cheer-events-public-v1":

								break;
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
		}
	}
}