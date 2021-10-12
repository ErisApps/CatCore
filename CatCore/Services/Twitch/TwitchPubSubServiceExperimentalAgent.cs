using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using CatCore.Models.Twitch.PubSub;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

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
		private readonly IKittenWebSocketProvider _kittenWebSocketProvider;

		private readonly Timer _pingTimer;
		private readonly Timer _pongTimer;

		private bool _hasPongBeenReceived;

		public TwitchPubSubServiceExperimentalAgent(ILogger logger, Random random, ITwitchAuthService twitchAuthService, IKittenPlatformActiveStateManager activeStateManager,
			string channelId)
		{
			_logger = logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, $"{(typeof(TwitchPubSubServiceExperimentalAgent)).FullName} ({channelId})");

			_random = random;
			_twitchAuthService = twitchAuthService;
			_activeStateManager = activeStateManager;
			_channelId = channelId;

			// TODO: Find a better way for this to ensure testability in the long run
			_kittenWebSocketProvider = new KittenWebSocketProvider(_logger); // manual resolution

			_pingTimer = new Timer {Interval = TWITCH_PUBSUB_PING_TIMER_DEFAULT_INTERVAL, AutoReset = false};
			_pingTimer.Elapsed += PingTimerOnElapsed;

			_pongTimer = new Timer { Interval = TWITCH_PUBSUB_PONG_TIMER_INTERVAL, AutoReset = false };
			_pongTimer.Elapsed += PongTimerOnElapsed;
		}

		// TODO: mark method as internal
		private async Task Start(bool force = false)
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

		private async Task Stop(string? disconnectReason = null)
		{
			await _kittenWebSocketProvider.Disconnect().ConfigureAwait(false);

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
		}

		public async ValueTask DisposeAsync()
		{
			_pingTimer.Dispose();
			_pongTimer.Dispose();

			await _kittenWebSocketProvider.Disconnect("Forced to go close").ConfigureAwait(false);
		}

		private void ConnectHappenedHandler()
		{
			_pingTimer.Start();
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
					case PubSubMessageTypes.RESPONSE:
						// TODO: Handle topic (de)registration responses.

						break;
					case PubSubMessageTypes.PONG:
						_hasPongBeenReceived = true;

						break;
					case PubSubMessageTypes.RECONNECT:
						Start().ConfigureAwait(false);

						break;
					case PubSubMessageTypes.MESSAGE:
						// TODO: Message handling code comes here
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

		internal void RequestTopicListening(string topic)
		{
			var fullTopic = ConvertTopic(topic);

			_logger.Information("Topic registration requested for (full) topic {FullTopic}", fullTopic);

			// TODO: Check if already started internally (and start when needed)
			// TODO: Check if already registered (both accepted and in-progress queues)
			// TODO: Send LISTEN request to wss
			// TODO: Keep track of in-progress LISTEN negotiations
		}

		internal void RequestTopicUnlistening(string topic)
		{
			var fullTopic = ConvertTopic(topic);

			_logger.Information("Topic de-registration requested for (full) topic {FullTopic}", fullTopic);

			// TODO: Check if topic was registered
			// TODO: Check if topic is already in-progress of being UNLISTEN-ed
			// TODO: Send UNLISTEN request to wss
			// TODO: Check if agent can be stopped internally
		}

		private string ConvertTopic(string topic)
		{
			return topic switch
			{
				PubSubTopics.VIDEO_PLAYBACK => PubSubTopics.FormatVideoPlaybackTopic(_channelId),
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

			_kittenWebSocketProvider.SendMessage(TWITCH_PUBSUB_PING_MESSAGE);
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
	}
}