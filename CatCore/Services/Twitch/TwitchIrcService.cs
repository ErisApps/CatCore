using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.EventArgs;
using CatCore.Models.Shared;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal class TwitchIrcService : ITwitchIrcService
	{
		private const string TWITCH_IRC_ENDPOINT = "wss://irc-ws.chat.twitch.tv:443";

		/// <remark>
		/// According to the official documentation, the rate limiting window interval is 30 seconds.
		/// However, due to delays in the connection/Twitch servers and this library being too precise time-wise,
		/// it might result in going over the rate limit again when it should have been reset.
		/// Resulting in a global temporary chat ban of 30 minutes, hence why we pick an internal time window of 32 seconds.
		/// </remark>
		private const long MESSAGE_SENDING_TIME_WINDOW_TICKS = 32 * TimeSpan.TicksPerSecond;

		private readonly ILogger _logger;
		private readonly IKittenWebSocketProvider _kittenWebSocketProvider;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;
		private readonly ITwitchRoomStateTrackerService _roomStateTrackerService;
		private readonly ITwitchUserStateTrackerService _userStateTrackerService;

		private readonly char[] _ircMessageSeparator;

		private readonly ConcurrentQueue<(string channelName, string message)> _messageQueue;
		private readonly ConcurrentDictionary<string, long> _forcedSendChannelMessageSendDelays;
		private readonly List<long> _messageSendTimestamps;

		private readonly SemaphoreSlim _workerCanSleepSemaphoreSlim = new SemaphoreSlim(1, 1);
		private readonly SemaphoreSlim _workerSemaphoreSlim = new SemaphoreSlim(0, 1);

		private CancellationTokenSource? _messageQueueProcessorCancellationTokenSource;

		public TwitchIrcService(ILogger logger, IKittenWebSocketProvider kittenWebSocketProvider, IKittenPlatformActiveStateManager activeStateManager, ITwitchAuthService twitchAuthService,
			ITwitchChannelManagementService twitchChannelManagementService, ITwitchRoomStateTrackerService roomStateTrackerService, ITwitchUserStateTrackerService userStateTrackerService)
		{
			_logger = logger;
			_kittenWebSocketProvider = kittenWebSocketProvider;
			_activeStateManager = activeStateManager;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;
			_roomStateTrackerService = roomStateTrackerService;
			_userStateTrackerService = userStateTrackerService;

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;
			_twitchChannelManagementService.ChannelsUpdated += TwitchChannelManagementServiceOnChannelsUpdated;

			_ircMessageSeparator = new[] {'\r', '\n'};

			_messageQueue = new ConcurrentQueue<(string channelName, string message)>();
			_forcedSendChannelMessageSendDelays = new ConcurrentDictionary<string, long>();
			_messageSendTimestamps = new List<long>();
		}

		public event Action? OnLogin;
		public event Action<IChatChannel>? OnJoinChannel;
		public event Action<IChatChannel>? OnLeaveChannel;
		public event Action<IChatChannel>? OnRoomStateChanged;
		public event Action<IChatMessage>? OnMessageReceived;

		public void SendMessage(IChatChannel channel, string message)
		{
			_workerCanSleepSemaphoreSlim.Wait();
			_messageQueue.Enqueue((channel.Id, $"@id={Guid.NewGuid().ToString()} {IrcCommands.PRIVMSG} #{channel.Id} :{message}"));
			_workerCanSleepSemaphoreSlim.Release();

			// Trigger re-activation of worker thread
			if (_workerSemaphoreSlim.CurrentCount == 0)
			{
				_workerSemaphoreSlim.Release();
			}
		}

		async Task ITwitchIrcService.Start()
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

			await _kittenWebSocketProvider.Connect(TWITCH_IRC_ENDPOINT).ConfigureAwait(false);
		}

		async Task ITwitchIrcService.Stop()
		{
			await _kittenWebSocketProvider.Disconnect("Requested by service manager").ConfigureAwait(false);

			_kittenWebSocketProvider.ConnectHappened -= ConnectHappenedHandler;
			_kittenWebSocketProvider.DisconnectHappened -= DisconnectHappenedHandler;
			_kittenWebSocketProvider.MessageReceived -= MessageReceivedHandler;
		}

		private async void TwitchAuthServiceOnOnCredentialsChanged()
		{
			if (_twitchAuthService.HasTokens)
			{
				if (_activeStateManager.GetState(PlatformType.Twitch))
				{
					await ((ITwitchIrcService) this).Start().ConfigureAwait(false);
				}
			}
			else
			{
				await ((ITwitchIrcService) this).Stop().ConfigureAwait(false);
			}
		}

		private void TwitchChannelManagementServiceOnChannelsUpdated(object sender, TwitchChannelsUpdatedEventArgs e)
		{
			if (_activeStateManager.GetState(PlatformType.Twitch))
			{
				foreach (var disabledChannel in e.DisabledChannels)
				{
					_kittenWebSocketProvider.SendMessage($"PART #{disabledChannel.Value}");
				}

				foreach (var enabledChannel in e.EnabledChannels)
				{
					_kittenWebSocketProvider.SendMessage($"JOIN #{enabledChannel.Value}");
				}
			}
		}

		private void ConnectHappenedHandler()
		{
			_kittenWebSocketProvider.SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");

			_kittenWebSocketProvider.SendMessage($"PASS oauth:{_twitchAuthService.AccessToken}");
			_kittenWebSocketProvider.SendMessage($"NICK {_twitchAuthService.LoggedInUser?.LoginName ?? "."}");
		}

		private void DisconnectHappenedHandler()
		{
			_messageQueueProcessorCancellationTokenSource?.Cancel();
			_messageQueueProcessorCancellationTokenSource = null;
		}

		private void MessageReceivedHandler(string message)
		{
			MessageReceivedHandlerInternal(message);
		}

		private void MessageReceivedHandlerInternal(string rawMessage, bool sendBySelf = false)
		{
			// TODO: Investigate possibility to split a message string into ReadOnlySpans<char> types instead of strings again, would prevents unnecessary heap allocations which might in turn improve the throughput
			var messages = rawMessage.Split(_ircMessageSeparator, StringSplitOptions.RemoveEmptyEntries);

			foreach (var messageInternal in messages)
			{
				// Handle IRC messages here
				IrcExtensions.ParseIrcMessage(messageInternal, out var tags, out var prefix, out string commandType, out var channelName, out var message);
#if DEBUG
				_logger.Verbose("Tags count: {Tags}", tags?.Count.ToString() ?? "N/A");
				_logger.Verbose("Prefix: {Prefix}", prefix ?? "N/A");
				_logger.Verbose("CommandType: {CommandType}", commandType);
				_logger.Verbose("ChannelName: {ChannelName}", channelName ?? "N/A");
				_logger.Verbose("Message: {Message}", message ?? "N/A");
				_logger.Verbose("");
#endif

				HandleParsedIrcMessage(ref tags, ref prefix, ref commandType, ref channelName, ref message, sendBySelf);
			}
		}


		// ReSharper disable once CognitiveComplexity
		// ReSharper disable once CyclomaticComplexity
		private void HandleParsedIrcMessage(ref ReadOnlyDictionary<string, string>? messageMeta, ref string? prefix, ref string commandType, ref string? channelName, ref string? message,
			bool wasSendByLibrary)
		{
			// Command official documentation: https://datatracker.ietf.org/doc/html/rfc1459 and https://datatracker.ietf.org/doc/html/rfc2812
			// Command Twitch documentation: https://dev.twitch.tv/docs/irc/commands
			// CommandMeta documentation: https://dev.twitch.tv/docs/irc/tags

			switch (commandType)
			{
				case IrcCommands.PING:
					_kittenWebSocketProvider.SendMessage($"{IrcCommands.PONG} :{message!}");
					break;
				case IrcCommands.RPL_ENDOFMOTD:
					OnLogin?.Invoke();
					foreach (var loginName in _twitchChannelManagementService.GetAllActiveLoginNames())
					{
						_kittenWebSocketProvider.SendMessage($"JOIN #{loginName}");
					}

					_messageQueueProcessorCancellationTokenSource?.Cancel();
					_messageQueueProcessorCancellationTokenSource = new CancellationTokenSource();

					_ = Task.Run(() => ProcessQueuedMessage(_messageQueueProcessorCancellationTokenSource.Token), _messageQueueProcessorCancellationTokenSource.Token).ConfigureAwait(false);

					// TODO: Remove this placeholder code... seriously... It's just here so the code would compile 😸
					if (prefix == "")
					{
						OnJoinChannel?.Invoke(null!);
						OnLeaveChannel?.Invoke(null!);
						OnRoomStateChanged?.Invoke(null!);
						OnMessageReceived?.Invoke(null!);
					}

					break;
				case IrcCommands.NOTICE:
					// MessageId for NOTICE documentation: https://dev.twitch.tv/docs/irc/msg-id
					switch (message)
					{
						case "Login authentication failed":
							_logger.Warning("Login failed. Error {ErrorMessage}", message);
							_kittenWebSocketProvider.Disconnect(message).ConfigureAwait(false);
							break;
					}

					goto case IrcCommands.PRIVMSG;
				case TwitchIrcCommands.USERNOTICE:
				case IrcCommands.PRIVMSG:
					HandlePrivMessage(ref messageMeta, ref prefix, ref commandType, ref channelName, ref message, wasSendByLibrary);

					break;
				case IrcCommands.JOIN:
				{
					prefix.ParsePrefix(out _, out _, out var username, out _);
					if (_twitchAuthService.LoggedInUser?.LoginName == username)
					{
						// TODO: pass channel object with correct Id
						OnJoinChannel?.Invoke(new TwitchChannel(channelName!, channelName!));
					}

					break;
				}
				case IrcCommands.PART:
				{
					prefix.ParsePrefix(out _, out _, out var username, out _);
					if (_twitchAuthService.LoggedInUser?.LoginName == username)
					{
						var roomState = _roomStateTrackerService.GetRoomState(channelName!);
						// TODO: pass channel object with guaranteed Id
						OnLeaveChannel?.Invoke(new TwitchChannel(roomState?.RoomId ?? string.Empty, channelName!));

						_roomStateTrackerService.UpdateRoomState(channelName!, null);
						_userStateTrackerService.UpdateUserState(channelName!, null);
					}

					break;
				}
				case TwitchIrcCommands.ROOMSTATE:
					var updatedRoomState = _roomStateTrackerService.UpdateRoomState(channelName!, messageMeta);
					// TODO: pass channel object with guaranteed Id
					OnRoomStateChanged?.Invoke(new TwitchChannel(updatedRoomState?.RoomId ?? "", channelName!));

					break;
				case TwitchIrcCommands.USERSTATE:
					_userStateTrackerService.UpdateUserState(channelName!, messageMeta);

					break;
				case TwitchIrcCommands.GLOBALUSERSTATE:
					_userStateTrackerService.UpdateGlobalUserState(messageMeta);

					break;
				case TwitchIrcCommands.CLEARCHAT:
					break;
				case TwitchIrcCommands.CLEARMSG:
					break;
				case TwitchIrcCommands.RECONNECT:
					((ITwitchIrcService) this).Start().ConfigureAwait(false);
					break;
				case TwitchIrcCommands.HOSTTARGET:
					// NOP
					// Consumers are already notified of this through a NOTICE message when the logged in user hosts/stops hosting a channel
					// Also doesn't cover when the user is the one getting hosted by another channel
					break;
			}
		}

		// ReSharper disable once CognitiveComplexity
		// ReSharper disable once CyclomaticComplexity
		private void HandlePrivMessage(ref ReadOnlyDictionary<string, string>? messageMeta, ref string? prefix, ref string commandType, ref string? channelName, ref string? message,
			bool wasSendByLibrary)
		{
			prefix.ParsePrefix(out var isServer, out var nickname, out var username, out var hostname);

			// Determine channelId
			string channelId = messageMeta != null && messageMeta.TryGetValue(IrcMessageTags.ROOM_ID, out var roomId)
				? roomId
				: _roomStateTrackerService.GetRoomState(channelName!)?.RoomId ?? "";

			// Create Channel object
			var channel = new TwitchChannel(channelId, channelName!);

			// Create sender object
			TwitchUser twitchUser;
			if (wasSendByLibrary)
			{
				var globalUserState = _userStateTrackerService.GlobalUserState!;
				var userState = _userStateTrackerService.GetUserState(channelName!);
				twitchUser = new TwitchUser(globalUserState?.UserId ?? _twitchAuthService.LoggedInUser?.UserId ?? "",
					_twitchAuthService.LoggedInUser?.LoginName ?? "",
					globalUserState?.DisplayName ?? _twitchAuthService.LoggedInUser?.LoginName ?? "",
					globalUserState?.Color ?? "#ffffff",
					userState?.IsModerator ?? false,
					userState?.IsBroadcaster ?? false,
					userState?.IsSubscriber ?? false,
					userState?.IsTurbo ?? false,
					userState?.IsVip ?? false);
			}
			else
			{
				string userId;
				string displayName;
				string color;

				bool isModerator;
				bool isBroadcaster;
				bool isSubscriber;
				bool isTurbo;
				bool isVip;

				if (messageMeta != null)
				{
					if (!messageMeta.TryGetValue(IrcMessageTags.USER_ID, out userId))
					{
						userId = string.Empty;
					}

					if (!messageMeta.TryGetValue(IrcMessageTags.DISPLAY_NAME, out displayName))
					{
						displayName = (bool) isServer! ? hostname! : username!;
					}

					if (!messageMeta.TryGetValue(IrcMessageTags.COLOR, out color))
					{
						color = "#ffffff";
					}

					if (messageMeta.TryGetValue(IrcMessageTags.BADGES, out var badgesString))
					{
						isModerator = badgesString.Contains("moderator/");
						isBroadcaster = badgesString.Contains("broadcaster/");
						isSubscriber = (badgesString.Contains("subscriber/")) || (badgesString.Contains("founder/"));
						isTurbo = badgesString.Contains("turbo/");
						isVip = badgesString.Contains("vip/");
					}
					else
					{
						isModerator = false;
						isBroadcaster = false;
						isSubscriber = false;
						isTurbo = false;
						isVip = false;
					}
				}
				else
				{
					userId = string.Empty;
					displayName = (bool) isServer! ? hostname! : username!;
					color = "#ffffff";
					isModerator = false;
					isBroadcaster = false;
					isSubscriber = false;
					isTurbo = false;
					isVip = false;
				}

				twitchUser = new TwitchUser(userId,
					(bool) isServer! ? hostname! : username!,
					displayName,
					color,
					isModerator,
					isBroadcaster,
					isSubscriber,
					isTurbo,
					isVip
				);
			}

			var isActionMessage = false;
			if (message?.StartsWith("ACTION ") ?? false)
			{
				isActionMessage = true;
				message = message.AsSpan().Slice(8, message.Length - 9).ToString();
			}

			OnMessageReceived?.Invoke(new TwitchMessage(
				messageMeta != null && messageMeta.TryGetValue(IrcMessageTags.ID, out var messageId) ? messageId : Guid.NewGuid().ToString(),
				commandType == IrcCommands.NOTICE || commandType == TwitchIrcCommands.USERNOTICE,
				isActionMessage,
				false,
				message ?? string.Empty,
				twitchUser,
				channel,
				messageMeta,
				commandType,
				0
			));
		}

		// ReSharper disable once CognitiveComplexity
		private async Task ProcessQueuedMessage(CancellationToken cts)
		{
			long? GetTicksTillReset()
			{
				if (_messageQueue.IsEmpty)
				{
					return null;
				}

				var rateLimit = (int) GetRateLimit(_messageQueue.First().channelName);

				if (_messageSendTimestamps.Count < rateLimit)
				{
					return 0;
				}

				var ticksTillReset = _messageSendTimestamps[_messageSendTimestamps.Count - rateLimit] + MESSAGE_SENDING_TIME_WINDOW_TICKS - DateTime.UtcNow.Ticks;
				return ticksTillReset > 0 ? ticksTillReset : 0;
			}

			void UpdateRateLimitState()
			{
				while (_messageSendTimestamps.Count > 0 && DateTime.UtcNow.Ticks - _messageSendTimestamps.First() > MESSAGE_SENDING_TIME_WINDOW_TICKS)
				{
					_messageSendTimestamps.RemoveAt(0);
				}
			}

			bool CheckIfConsumable()
			{
				UpdateRateLimitState();

				return !_messageQueue.IsEmpty && _messageSendTimestamps.Count < (int) GetRateLimit(_messageQueue.First().channelName);
			}

			async Task HandleQueue()
			{
				while (_messageQueue.TryPeek(out var msg))
				{
					var rateLimit = GetRateLimit(msg.channelName);
					if (_messageSendTimestamps.Count >= (int) rateLimit)
					{
						_logger.Debug("Hit rate limit. Type {RateLimit}", rateLimit.ToString("G"));
						break;
					}

					if (_forcedSendChannelMessageSendDelays.TryGetValue(msg.channelName, out var ticksSinceLastChannelMessage))
					{
						var ticksTillReset = ticksSinceLastChannelMessage + (rateLimit == MessageSendingRateLimit.Relaxed ? 50 : 1250) * TimeSpan.TicksPerMillisecond - DateTime.UtcNow.Ticks;
						if (ticksTillReset > 0)
						{
							var msTillReset = (int) Math.Ceiling((double) ticksTillReset / TimeSpan.TicksPerMillisecond);
							_logger.Verbose("Delayed message sending, will send next message in {TimeTillReset}ms", msTillReset);
							await Task.Delay(msTillReset, CancellationToken.None).ConfigureAwait(false);
						}
					}

					_messageQueue.TryDequeue(out msg);

					// Send message
					await _kittenWebSocketProvider.SendMessageInstant(msg.message).ConfigureAwait(false);
					// TODO: Add forwarding to internal message received handler
					// _logger.Information(msg.message);

					var ticksNow = DateTime.UtcNow.Ticks;
					_messageSendTimestamps.Add(ticksNow);
					_forcedSendChannelMessageSendDelays[msg.channelName] = ticksNow;
				}
			}

			while (!cts.IsCancellationRequested)
			{
				await HandleQueue().ConfigureAwait(false);

				do
				{
					_logger.Verbose("Hibernating worker queue");

					await _workerCanSleepSemaphoreSlim.WaitAsync(CancellationToken.None).ConfigureAwait(false);
					var canConsume = !_messageQueue.IsEmpty;
					_workerCanSleepSemaphoreSlim.Release();

					var remainingTicks = GetTicksTillReset();
					var autoReExecutionDelay = canConsume ? remainingTicks > 0 ? (int) Math.Ceiling((double) remainingTicks / TimeSpan.TicksPerMillisecond) : 0 : -1;
					_logger.Information("Auto re-execution delay: {AutoReExecutionDelay}ms", autoReExecutionDelay);

					await Task.WhenAny(
							Task.Delay(autoReExecutionDelay, cts),
							_workerSemaphoreSlim.WaitAsync(cts))
						.ConfigureAwait(false);

					_logger.Verbose("Waking up worker queue");
				} while (!CheckIfConsumable() && !cts.IsCancellationRequested);
			}

			_logger.Warning("Stopped worker queue");
		}

		private MessageSendingRateLimit GetRateLimit(string channelName)
		{
			if (_twitchAuthService.LoggedInUser?.LoginName == channelName)
			{
				return MessageSendingRateLimit.Relaxed;
			}

			var userState = _userStateTrackerService.GetUserState(channelName);
			if (userState != null && (userState.IsBroadcaster || userState.IsModerator))
			{
				return MessageSendingRateLimit.Relaxed;
			}

			return MessageSendingRateLimit.Normal;
		}
	}
}