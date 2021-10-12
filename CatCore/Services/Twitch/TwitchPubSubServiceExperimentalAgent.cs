﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CatCore.Helpers;
using CatCore.Models.Twitch.PubSub;
using CatCore.Models.Twitch.PubSub.Requests;
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
		private readonly IKittenWebSocketProvider _kittenWebSocketProvider;

		private readonly HashSet<string> _acceptedTopics = new();
		private readonly ConcurrentDictionary<string, string> _inProgressTopicNegotiations = new();
		private readonly ConcurrentQueue<(string mode, string topic)> _topicNegotiationQueue = new();

		private readonly SemaphoreSlim _workerSemaphoreSlim = new(0, 1);

		private CancellationTokenSource? _topicNegotiationQueueProcessorCancellationTokenSource;

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


			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
			_kittenWebSocketProvider.MessageReceived += MessageReceivedHandler;

			await _kittenWebSocketProvider.Connect(TWITCH_PUBSUB_ENDPOINT).ConfigureAwait(false);
		}

		private async Task Stop(string? disconnectReason = null)
		{
			await _kittenWebSocketProvider.Disconnect().ConfigureAwait(false);

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
		}

		public async ValueTask DisposeAsync()
		{
			await Stop("Forced to go close").ConfigureAwait(false);

			_pingTimer.Dispose();
			_pongTimer.Dispose();
		}

		private void ConnectHappenedHandler()
		{
			_pingTimer.Start();

			_topicNegotiationQueueProcessorCancellationTokenSource?.Cancel();
			_topicNegotiationQueueProcessorCancellationTokenSource = new CancellationTokenSource();

			_ = Task.Run(() => ProcessQueuedTopicNegotiationMessage(_topicNegotiationQueueProcessorCancellationTokenSource.Token), _topicNegotiationQueueProcessorCancellationTokenSource.Token)
				.ConfigureAwait(false);
		}

		// ReSharper disable once CognitiveComplexity
		// ReSharper disable once CyclomaticComplexity
		private void MessageReceivedHandler(string receivedMessage)
		{
#if DEBUG
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
		private async Task ProcessQueuedTopicNegotiationMessage(CancellationToken cts)
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

					var jsonMessage = JsonSerializer.Serialize(new TopicNegotiationMessage(mode, new TopicNegotiationMessageData(new[] { fullTopic }), nonce));

					_inProgressTopicNegotiations.TryAdd(nonce, msg.topic);

					_logger.Information("Sending {Mode} request for topic {Topic} with nonce {Nonce}", mode, msg.topic, nonce);

					// Send message
					await _kittenWebSocketProvider.SendMessageInstant(jsonMessage).ConfigureAwait(false);
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