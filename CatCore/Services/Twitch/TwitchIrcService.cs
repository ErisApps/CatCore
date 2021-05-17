using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CatCore.Models.EventArgs;
using CatCore.Models.Shared;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;
using Websocket.Client;
using Websocket.Client.Models;

namespace CatCore.Services.Twitch
{
	internal class TwitchIrcService : ITwitchIrcService
	{
		private const string TWITCH_IRC_ENDPOINT = "wss://irc-ws.chat.twitch.tv:443";

		private readonly ILogger _logger;
		private readonly IKittenWebSocketProvider _kittenWebSocketProvider;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;

		private readonly char[] _ircMessageSeparator;

		public TwitchIrcService(ILogger logger, IKittenWebSocketProvider kittenWebSocketProvider, IKittenPlatformActiveStateManager activeStateManager, ITwitchAuthService twitchAuthService,
			ITwitchChannelManagementService twitchChannelManagementService)
		{
			_logger = logger;
			_kittenWebSocketProvider = kittenWebSocketProvider;
			_activeStateManager = activeStateManager;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;
			_twitchChannelManagementService.ChannelsUpdated += TwitchChannelManagementServiceOnChannelsUpdated;

			_ircMessageSeparator = new[] {'\r', '\n'};
		}

		public event Action? OnLogin;
		public event Action<IChatChannel>? OnJoinChannel;
		public event Action<IChatChannel>? OnLeaveChannel;
		public event Action<IChatChannel>? OnRoomStateChanged;
		public event Action<IChatMessage>? OnMessageReceived;

		public void SendMessage(IChatChannel channel, string message)
		{
			// TODO: Add actual global rate limiting. 100msg/30s when broadcaster/moderator on channel, 20msg/30s when otherwise
			_kittenWebSocketProvider.SendMessage($"@id={Guid.NewGuid().ToString()} {IrcCommands.PRIVMSG} #{channel.Id} :{message}");
		}

		async Task ITwitchIrcService.Start()
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

			await _kittenWebSocketProvider.Connect(TWITCH_IRC_ENDPOINT).ConfigureAwait(false);
		}

		async Task ITwitchIrcService.Stop()
		{
			await _kittenWebSocketProvider.Disconnect("Requested by service manager").ConfigureAwait(false);

			_kittenWebSocketProvider.ReconnectHappened -= ReconnectHappenedHandler;
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

		private void ReconnectHappenedHandler(ReconnectionInfo info)
		{
			_logger.Debug("(Re)connect happened - {Url} - {Type}", TWITCH_IRC_ENDPOINT, info.Type);

			_kittenWebSocketProvider.SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");

			_kittenWebSocketProvider.SendMessage($"PASS oauth:{_twitchAuthService.AccessToken}");
			_kittenWebSocketProvider.SendMessage($"NICK {_twitchAuthService.LoggedInUser?.LoginName ?? "."}");
		}

		private void DisconnectHappenedHandler(DisconnectionInfo info)
		{
			_logger.Information("Closed connection to Twitch IRC server");
		}

		private void MessageReceivedHandler(ResponseMessage response)
		{
			MessageReceivedHandlerInternal(response.Text);
		}

		private void MessageReceivedHandlerInternal(string rawMessage)
		{
			// TODO: Investigate possibility to split a message string into ReadOnlySpans<char> types instead of strings again, would prevents unnecessary heap allocations which might in turn improve the throughput
			var messages = rawMessage.Split(_ircMessageSeparator, StringSplitOptions.RemoveEmptyEntries);

			foreach (var messageInternal in messages)
			{
				// Handle IRC messages here
				ParseIrcMessage(messageInternal, out var tags, out var prefix, out string commandType, out var channelName, out var message);
#if DEBUG
				_logger.Verbose("Tags count: {Tags}", tags?.Count.ToString() ?? "N/A");
				_logger.Verbose("Prefix: {Prefix}", prefix ?? "N/A");
				_logger.Verbose("CommandType: {CommandType}", commandType);
				_logger.Verbose("ChannelName: {ChannelName}", channelName ?? "N/A");
				_logger.Verbose("Message: {Message}", message ?? "N/A");
				_logger.Verbose("");
#endif

				HandleParsedIrcMessage(ref tags, ref prefix, ref commandType, ref channelName, ref message);
			}
		}

		// ReSharper disable once CognitiveComplexity
		private static void ParseIrcMessage(string messageInternal, out ReadOnlyDictionary<string, string>? tags, out string? prefix, out string commandType, out string? channelName, out string? message)
		{
			// Null-ing this here as I can't do that in the method signature
			tags = null;
			prefix = null;
			channelName = null;
			message = null;

			// Twitch IRC Message spec
			// https://ircv3.net/specs/extensions/message-tags

			var position = 0;
			int nextSpacePosition;

			var messageAsSpan = messageInternal.AsSpan();

			void SkipToNextNonSpaceCharacter(ref ReadOnlySpan<char> msg)
			{
				while (position < msg.Length && msg[position] == ' ')
				{
					position++;
				}
			}

			// Check for message tags
			if (messageAsSpan[0] == '@')
			{
				nextSpacePosition = messageAsSpan.IndexOf(' ');
				if (nextSpacePosition == -1)
				{
					throw new Exception("Invalid IRC Message");
				}

				var tagsAsSpan = messageAsSpan.Slice(1, nextSpacePosition - 1);

				var tagsDictInternal = new Dictionary<string, string>();

				var charSeparator = '=';
				var startPos = 0;

				ReadOnlySpan<char> keyTmp = null;
				for (var curPos = 0; curPos < tagsAsSpan.Length; curPos++)
				{
					if (tagsAsSpan[curPos] == charSeparator)
					{
						if (charSeparator == ';')
						{
							if (curPos != startPos)
							{
								tagsDictInternal[keyTmp.ToString()] = tagsAsSpan.Slice(startPos, curPos - startPos).ToString();
							}

							charSeparator = '=';
							startPos = curPos + 1;
						}
						else
						{
							keyTmp = tagsAsSpan.Slice(startPos, curPos - startPos);

							charSeparator = ';';
							startPos = curPos + 1;
						}
					}
				}

				tags = new ReadOnlyDictionary<string, string>(tagsDictInternal);

				position = nextSpacePosition + 1;
				SkipToNextNonSpaceCharacter(ref messageAsSpan);
				messageAsSpan = messageAsSpan.Slice(position);
				position = 0;
			}


			// Handle prefix
			if (messageAsSpan[position] == ':')
			{
				nextSpacePosition = messageAsSpan.IndexOf(' ');
				if (nextSpacePosition == -1)
				{
					throw new Exception("Invalid IRC Message");
				}

				prefix = messageAsSpan.Slice(1, (nextSpacePosition) - 1).ToString();

				position = nextSpacePosition + 1;
				SkipToNextNonSpaceCharacter(ref messageAsSpan);
				messageAsSpan = messageAsSpan.Slice(position);
				position = 0;
			}


			// Handle MessageType
			nextSpacePosition = messageAsSpan.IndexOf(' ');
			if (nextSpacePosition == -1)
			{
				if (messageAsSpan.Length > position)
				{
					commandType = messageAsSpan.ToString();
					return;
				}
			}

			commandType = messageAsSpan.Slice(0, nextSpacePosition).ToString();

			position = nextSpacePosition + 1;
			SkipToNextNonSpaceCharacter(ref messageAsSpan);
			messageAsSpan = messageAsSpan.Slice(position);
			position = 0;


			// Handle channelname and message
			var handledInLoop = false;
			while (position < messageAsSpan.Length)
			{
				if (messageAsSpan[position] == ':')
				{
					handledInLoop = true;

					// Handle message (extracting this first as we're going to do a lookback in order to determine the previous part)
					message = messageAsSpan.Slice(position + 1).ToString();

					// Handle everything before the colon as the channelname parameter
					while (--position > 0 && messageAsSpan[position] == ' ')
					{
					}

					if (position > 0)
					{
						channelName = messageAsSpan.Slice(0, position + 1).ToString();
					}

					break;
				}

				position++;
			}

			if (!handledInLoop)
			{
				channelName = messageAsSpan.ToString();
			}
		}

		// ReSharper disable once CognitiveComplexity
		// ReSharper disable once CyclomaticComplexity
		private void HandleParsedIrcMessage(ref ReadOnlyDictionary<string, string>? messageMeta, ref string? prefix, ref string commandType, ref string? channelName, ref string? message)
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

					break;
				case TwitchIrcCommands.USERNOTICE:
				case IrcCommands.PRIVMSG:
					break;
				case IrcCommands.JOIN:
					break;
				case IrcCommands.PART:
					break;
				case TwitchIrcCommands.ROOMSTATE:
					break;
				case TwitchIrcCommands.USERSTATE:
					break;
				case TwitchIrcCommands.GLOBALUSERSTATE:
					break;
				case TwitchIrcCommands.CLEARCHAT:
					break;
				case TwitchIrcCommands.CLEARMSG:
					break;
				case TwitchIrcCommands.RECONNECT:
					break;
				case TwitchIrcCommands.HOSTTARGET:
					break;
			}
		}
	}
}