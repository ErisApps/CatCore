using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CatCore.Helpers;
using CatCore.Helpers.JSON;
using CatCore.Models.Shared;
using CatCore.Models.Twitch.PubSub;
using CatCore.Models.Twitch.PubSub.Requests;
using CatCore.Models.Twitch.PubSub.Responses;
using CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1;
using CatCore.Models.Twitch.PubSub.Responses.Polls;
using CatCore.Models.Twitch.PubSub.Responses.Predictions;
using CatCore.Models.Twitch.PubSub.Responses.VideoPlayback;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;
using Timer = System.Timers.Timer;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchPubSubServiceExperimentalAgent : IAsyncDisposable
	{
		private const string TWITCH_PUBSUB_ENDPOINT = "wss://pubsub-edge.twitch.tv";
		private const string TWITCH_PUBSUB_PING_MESSAGE = @"{""type"": ""PING""}";

		// According to the Twitch PubSub docs, we should send a ping message at least once every 5 minutes
		// => Sending one every 2 or 3 minutes sounds like a sane number.
		private const int TWITCH_PUBSUB_PING_TIMER_DEFAULT_INTERVAL = 120 * 1000;

		// We should also check whether we get a response within 10 seconds after the message has been send.
		// => Adding an additional 5 second buffer to the pong timer in case the connection is a bit slower though
		private const int TWITCH_PUBSUB_PONG_TIMER_INTERVAL = 15 * 1000;

		private const string VALID_NONCE_CHARS = "abcdefghijklmnopqrstuvwxyz0123456789";
		private const int VALID_NONCE_CHARS_LENGTH = 36;
		private const int GENERATED_NONCE_LENGTH = 16;

		private readonly ILogger _logger;
		private readonly Random _random;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly string _channelId;
		private readonly HashSet<string> _activeTopicsInManager;

		private readonly IKittenWebSocketProvider _kittenWebSocketProvider;

		private readonly SemaphoreSlim _wsStateChangeSemaphoreSlim = new(1, 1);
		private readonly SemaphoreSlim _initSemaphoreSlim = new(1, 1);

		private readonly HashSet<string> _acceptedTopics = new();
		private readonly ConcurrentDictionary<string, string> _inProgressTopicNegotiations = new();
		private readonly ConcurrentQueue<(string mode, string topic)> _topicNegotiationQueue = new();

		private readonly SemaphoreSlim _workerSemaphoreSlim = new(0, 1);

		private WebSocketConnection? _webSocketConnection;
		private CancellationTokenSource? _topicNegotiationQueueProcessorCancellationTokenSource;

		private readonly Timer _pingTimer;
		private readonly Timer _pongTimer;

		private bool _hasPongBeenReceived;

		public TwitchPubSubServiceExperimentalAgent(ILogger logger, Random random, ITwitchAuthService twitchAuthService, IKittenPlatformActiveStateManager activeStateManager,
			string channelId, HashSet<string> activeTopicsInManager)
		{
			_logger = logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, $"{(typeof(TwitchPubSubServiceExperimentalAgent)).FullName} ({channelId})");

			_random = random;
			_twitchAuthService = twitchAuthService;
			_activeStateManager = activeStateManager;
			_channelId = channelId;
			_activeTopicsInManager = activeTopicsInManager;

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;

			// TODO: Find a better way for this to ensure testability in the long run
			_kittenWebSocketProvider = new KittenWebSocketProvider(_logger); // manual resolution

			_pingTimer = new Timer { Interval = TWITCH_PUBSUB_PING_TIMER_DEFAULT_INTERVAL, AutoReset = false };
			_pingTimer.Elapsed += PingTimerOnElapsed;

			_pongTimer = new Timer { Interval = TWITCH_PUBSUB_PONG_TIMER_INTERVAL, AutoReset = false };
			_pongTimer.Elapsed += PongTimerOnElapsed;
		}

		internal event Action<string, StreamUp>? OnStreamUp;
		internal event Action<string, StreamDown>? OnStreamDown;
		internal event Action<string, ViewCountUpdate>? OnViewCountUpdate;
		internal event Action<string, Commercial>? OnCommercial;

		internal event Action<string, Follow>? OnFollow;
		internal event Action<string, PollData>? OnPoll;
		internal event Action<string, PredictionData>? OnPrediction;
		internal event Action<string, RewardRedeemedData>? OnRewardRedeemed;

		private async void TwitchAuthServiceOnOnCredentialsChanged()
		{
			if (_twitchAuthService.HasTokens)
			{
				if (_activeStateManager.GetState(PlatformType.Twitch))
				{
					await Start().ConfigureAwait(false);
				}
			}
			else
			{
				await Stop().ConfigureAwait(false);
			}
		}

		// ReSharper disable once CognitiveComplexity
		private async Task Start(bool force = false)
		{
			if (!force && (_initSemaphoreSlim.CurrentCount == 0 || _kittenWebSocketProvider.IsConnected))
			{
				return;
			}

			var lockAcquired = false;

			try
			{
				if (!(lockAcquired = await _initSemaphoreSlim.WaitAsync(force ? -1 : 0).ConfigureAwait(false)))
				{
					return;
				}

				using var _ = await Synchronization.LockAsync(_wsStateChangeSemaphoreSlim).ConfigureAwait(false);

				if (!force && _kittenWebSocketProvider.IsConnected)
				{
					return;
				}

				if (!_twitchAuthService.HasTokens)
				{
					return;
				}

				var loggedInUser = await _twitchAuthService.FetchLoggedInUserInfoWithRefresh().ConfigureAwait(false);
				if (loggedInUser == null)
				{
					return;
				}

				_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
				_kittenWebSocketProvider.ConnectHappened += ConnectHappenedHandler;

				_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
				_kittenWebSocketProvider.DisconnectHappened += DisconnectHappenedHandler;

				_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
				_kittenWebSocketProvider.MessageReceived += MessageReceivedHandler;

				await _kittenWebSocketProvider.Connect(TWITCH_PUBSUB_ENDPOINT).ConfigureAwait(false);
			}
			finally
			{
				if (lockAcquired)
				{
					_initSemaphoreSlim.Release();
				}
			}
		}

		private async Task Stop(string? disconnectReason = null)
		{
			using var _ = await Synchronization.LockAsync(_wsStateChangeSemaphoreSlim).ConfigureAwait(false);

			_topicNegotiationQueueProcessorCancellationTokenSource?.Cancel();
			_topicNegotiationQueueProcessorCancellationTokenSource = null;

			_pingTimer.Stop();
			_pongTimer.Stop();

			await _kittenWebSocketProvider.Disconnect(disconnectReason).ConfigureAwait(false);

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
		}

		public async ValueTask DisposeAsync()
		{
			await Stop("Forced to go close").ConfigureAwait(false);

			_pingTimer.Dispose();
			_pongTimer.Dispose();
		}

		private Task ConnectHappenedHandler(WebSocketConnection webSocketConnection)
		{
			_pingTimer.Start();

			_topicNegotiationQueueProcessorCancellationTokenSource?.Cancel();
			_topicNegotiationQueueProcessorCancellationTokenSource = new CancellationTokenSource();

			_webSocketConnection = webSocketConnection;
			_ = Task.Run(() => ProcessQueuedTopicNegotiationMessage(webSocketConnection, _topicNegotiationQueueProcessorCancellationTokenSource.Token), _topicNegotiationQueueProcessorCancellationTokenSource.Token)
				.ConfigureAwait(false);

			return Task.CompletedTask;
		}

		private async Task DisconnectHappenedHandler()
		{
			using var wsStateChangeLock = await Synchronization.LockAsync(_wsStateChangeSemaphoreSlim).ConfigureAwait(false);

			_webSocketConnection = null;

			_topicNegotiationQueueProcessorCancellationTokenSource?.Cancel();
			_topicNegotiationQueueProcessorCancellationTokenSource = null;

			_pingTimer.Stop();
			_pongTimer.Stop();

			// Prepare and reorder topic negotiation queue
			_acceptedTopics.Clear();

			while (_topicNegotiationQueue.TryDequeue(out _))
			{
			}

			var activeTopicsInManager = _activeTopicsInManager.ToList();
			foreach (var activeTopic in activeTopicsInManager)
			{
				_logger.Debug("Re-queued topic LISTEN registration for topic {Topic}", activeTopic);
				_topicNegotiationQueue.Enqueue((TopicNegotiationMessage.LISTEN, activeTopic));
			}
		}

		// ReSharper disable once CognitiveComplexity
		// ReSharper disable once CyclomaticComplexity
		private Task MessageReceivedHandler(WebSocketConnection webSocketConnection, string receivedMessage)
		{
#if !RELEASE
			var stopWatch = new System.Diagnostics.Stopwatch();
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
					case PubSubMessageTypes.RESPONSE:
						var error = rootElement.GetProperty("error").GetString()!;
						var nonce = rootElement.GetProperty("nonce").GetString()!;

						_inProgressTopicNegotiations.TryGetValue(nonce, out var topicSubscription);

						if (string.IsNullOrWhiteSpace(error))
						{
							if (_acceptedTopics.Contains(topicSubscription))
							{
								_acceptedTopics.Remove(topicSubscription);
								_logger.Verbose("UNLISTEN request with nonce {Nonce} for topic {Topic} got ACK'ed", nonce, topicSubscription);
							}
							else
							{
								_acceptedTopics.Add(topicSubscription);
								_logger.Verbose("LISTEN request with nonce {Nonce} for topic {Topic} got ACK'ed", nonce, topicSubscription);
							}
						}
						else
						{
							_logger.Warning("Topic (de)registration failed. Nonce: {Nonce}, error: {Error}", nonce, error);
						}

						_inProgressTopicNegotiations.TryRemove(nonce, out _);

						if (_workerSemaphoreSlim.CurrentCount == 0)
						{
							_workerSemaphoreSlim.Release();
						}

						break;
					case PubSubMessageTypes.PONG:
						_hasPongBeenReceived = true;

						break;
					case PubSubMessageTypes.RECONNECT:
						Start(true).ConfigureAwait(false);

						break;
					case PubSubMessageTypes.MESSAGE:
						// TODO: Message handling code comes here
						HandleMessageTypeInternal(rootElement);
						break;
				}
			}
			catch (Exception e)
			{
				_logger.Error(e, "An error occurred while trying to parse a PubSub message");
			}

#if !RELEASE
			stopWatch.Stop();
			_logger.Information("Handling of PubSub message took {ElapsedTime} ticks", stopWatch.ElapsedTicks);
#endif

			return Task.CompletedTask;
		}

		internal void RequestTopicListening(string topic)
		{
			Start().ConfigureAwait(false);

			QueueTopicNegotiation(TopicNegotiationMessage.LISTEN, topic);

			// TODO: 1) Check if already started internally (and start when needed)
			// TODO: 2) Check if already registered (both accepted and in-progress queues)
			// TODO: 3) Send LISTEN request to wss
			// TODO: 4) Keep track of in-progress LISTEN negotiations
			// Possible conflicts:
			// LISTEN and UNLISTEN requests could theoretically be send concurrently due to (un)subscribing from the higher-level event handlers
			// This could possibly lead to race conditions as topic negotiations (both LISTEN and UNLISTEN) need to be ACK'd

			_logger.Information("Topic registration requested for (full) topic {FullTopic}", ConvertTopic(topic));
		}

		internal void RequestTopicUnlistening(string topic)
		{
			QueueTopicNegotiation(TopicNegotiationMessage.UNLISTEN, topic);

			// TODO: 1) Check if topic was registered/accepted
			// TODO: 2) Check if topic is already in-progress of being UNLISTEN-ed
			// TODO: 3) Send UNLISTEN request to wss
			// TODO: 4) Check if agent can be stopped internally
			// Possible conflicts:
			// LISTEN and UNLISTEN requests could theoretically be send concurrently due to (un)subscribing from the higher-level event handlers
			// This could possibly lead to race conditions as topic negotiations (both LISTEN and UNLISTEN) need to be ACK'd

			_logger.Information("Topic de-registration requested for (full) topic {FullTopic}", ConvertTopic(topic));
		}

		private void QueueTopicNegotiation(string mode, string topic)
		{
			_topicNegotiationQueue.Enqueue((mode, topic));

			// Trigger re-activation of worker thread
			if (_workerSemaphoreSlim.CurrentCount == 0)
			{
				_workerSemaphoreSlim.Release();
			}
		}

		// ReSharper disable once CognitiveComplexity
		private async Task ProcessQueuedTopicNegotiationMessage(WebSocketConnection webSocketConnection, CancellationToken cts)
		{
			bool CheckIfConsumable()
			{
				return !_topicNegotiationQueue.IsEmpty;
			}

			async Task HandleQueue()
			{
				while (!cts.IsCancellationRequested && _topicNegotiationQueue.TryPeek(out var msg))
				{
					if (_inProgressTopicNegotiations.Values.Contains(msg.topic))
					{
						break;
					}

					_topicNegotiationQueue.TryDequeue(out msg);

					var mode = msg.mode;
					var fullTopic = ConvertTopic(msg.topic);
					var nonce = GenerateNonce();

					var jsonMessage = JsonSerializer.Serialize(
						new TopicNegotiationMessage(mode, new TopicNegotiationMessageData(new[] { fullTopic }, _twitchAuthService.AccessToken), nonce),
						TwitchPubSubSerializerContext.Default.TopicNegotiationMessage);

					_inProgressTopicNegotiations.TryAdd(nonce, msg.topic);

					_logger.Information("Sending {Mode} request for topic {Topic} with nonce {Nonce}", mode, msg.topic, nonce);

					// Send message
					await webSocketConnection.SendMessage(jsonMessage).ConfigureAwait(false);
				}
			}

			while (!cts.IsCancellationRequested)
			{
				await HandleQueue().ConfigureAwait(false);

				do
				{
					_logger.Verbose("Hibernating worker queue");

					await Task.WhenAny(
							Task.Delay(-1, cts),
							_workerSemaphoreSlim.WaitAsync(cts))
						.ConfigureAwait(false);

					_logger.Verbose("Waking up worker queue");
				} while (!CheckIfConsumable() && !cts.IsCancellationRequested);
			}

			_logger.Warning("Stopped worker queue");
		}

		private string ConvertTopic(string topic)
		{
			return topic switch
			{
				PubSubTopics.VIDEO_PLAYBACK => PubSubTopics.FormatVideoPlaybackTopic(_channelId),
				PubSubTopics.FOLLOWING => PubSubTopics.FormatFollowingTopic(_channelId),
				PubSubTopics.POLLS => PubSubTopics.FormatPollsTopic(_channelId),
				PubSubTopics.PREDICTIONS => PubSubTopics.FormatPredictionsTopic(_channelId),
				PubSubTopics.CHANNEL_POINTS_CHANNEL_V1 => PubSubTopics.FormatChannelPointsChannelV1Topic(_channelId),
				_ => throw new NotSupportedException()
			};
		}

		private string GenerateNonce()
		{
			var stringChars = new char[GENERATED_NONCE_LENGTH];
			for (var i = 0; i < GENERATED_NONCE_LENGTH; i++)
			{
				stringChars[i] = VALID_NONCE_CHARS[_random.Next(VALID_NONCE_CHARS_LENGTH)];
			}

			return new string(stringChars);
		}

		private void PingTimerOnElapsed(object sender, ElapsedEventArgs _)
		{
			_pongTimer.Stop();
			_pongTimer.Start();

			_hasPongBeenReceived = false;

			_webSocketConnection?.SendMessage(TWITCH_PUBSUB_PING_MESSAGE);
		}

		private async void PongTimerOnElapsed(object sender, ElapsedEventArgs _)
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

		// ReSharper disable once CognitiveComplexity
		private void HandleMessageTypeInternal(JsonElement rootElement)
		{
			var data = rootElement.GetProperty("data");

			var topicSpan = data.GetProperty("topic").GetString()!.AsSpan();
			var firstDotSeparator = topicSpan.IndexOf('.');
			var topic = topicSpan.Slice(0, firstDotSeparator).ToString();
			var message = data.GetProperty("message").GetString()!;

			switch (topic)
			{
				case PubSubTopics.VIDEO_PLAYBACK:
				{
					var videoFeedbackDocument = JsonDocument.Parse(message).RootElement;
					var internalType = videoFeedbackDocument.GetProperty("type").GetString();
					var serverTimeRaw = videoFeedbackDocument.GetProperty("server_time").GetRawText();

					switch (internalType)
					{
						case PubSubTopics.VideoPlaybackSubTopics.VIEW_COUNT:
							// available properties: serverTime, viewers
							// {"type":"viewcount","server_time":1634247120.108903,"viewers":36}
							OnViewCountUpdate?.Invoke(_channelId, new ViewCountUpdate(serverTimeRaw, videoFeedbackDocument.GetProperty("viewers").GetUInt32()));

							// _logger.Debug("Main event type: {MainType} - Internal event type: {SubType} - Viewers: {Viewers}", topic, internalType, viewers);

							break;
						case PubSubTopics.VideoPlaybackSubTopics.STREAM_UP:
							OnStreamUp?.Invoke(_channelId, new StreamUp(serverTimeRaw, videoFeedbackDocument.GetProperty("play_delay").GetInt32()));

							// _logger.Debug("Main event type: {MainType} - Internal event type: {SubType} - Play delay: {PlayDelay}", topic, internalType, playDelay);

							break;
						case PubSubTopics.VideoPlaybackSubTopics.STREAM_DOWN:
							// _logger.Debug("Main event type: {MainType} - Internal event type: {SubType}", topic, internalType);

							OnStreamDown?.Invoke(_channelId, new StreamDown(serverTimeRaw));

							break;
						case PubSubTopics.VideoPlaybackSubTopics.COMMERCIAL:
							OnCommercial?.Invoke(_channelId, new Commercial(serverTimeRaw, videoFeedbackDocument.GetProperty("length").GetUInt32()));

							// _logger.Debug("Main event type: {MainType} - Internal event type: {SubType} - Length: {Length}", topic, internalType, length);

							break;
					}

					break;
				}
				case PubSubTopics.FOLLOWING:
				{
					var follow = JsonSerializer.Deserialize(message, TwitchPubSubSerializerContext.Default.Follow);
					OnFollow?.Invoke(_channelId, follow);

					break;
				}
				case PubSubTopics.POLLS:
				{
					var pollsDocument = JsonDocument.Parse(message).RootElement;
					// var internalType = pollsDocument.GetProperty("type").GetString()!;
					var pollData = pollsDocument.GetProperty("data").GetProperty("poll").Deserialize(TwitchPubSubSerializerContext.Default.PollData);

					OnPoll?.Invoke(_channelId, pollData);

					break;
				}
				case PubSubTopics.PREDICTIONS:
				{
					var predictionsDocument = JsonDocument.Parse(message).RootElement;
					// var internalType = predictionsDocument.GetProperty("type").GetString()!;
					var predictionData = predictionsDocument.GetProperty("data").GetProperty("event").Deserialize(TwitchPubSubSerializerContext.Default.PredictionData);

					OnPrediction?.Invoke(_channelId, predictionData);

					break;
				}
				case PubSubTopics.CHANNEL_POINTS_CHANNEL_V1:
				{
					var channelPointsChannelDocument = JsonDocument.Parse(message).RootElement;
					var internalType = channelPointsChannelDocument.GetProperty("type").GetString()!;
					switch (internalType)
					{
						case PubSubTopics.ChannelPointsChannelV1SubTopics.REWARD_REDEEMED:
							OnRewardRedeemed?.Invoke(_channelId, channelPointsChannelDocument.GetProperty("data").GetProperty("redemption").Deserialize(TwitchPubSubSerializerContext.Default.RewardRedeemedData));
							break;
						default:
							_logger.Warning("Unhandled subtopic {SubTopic} for PubSub topic {Topic}", internalType, topic);
							break;
					}

					break;
				}
			}
		}
	}
}