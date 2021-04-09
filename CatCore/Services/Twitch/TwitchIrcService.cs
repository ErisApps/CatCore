using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CatCore.Services.Twitch.Interfaces;
using Serilog;
using Websocket.Client;
using Websocket.Client.Models;

namespace CatCore.Services.Twitch
{
	internal class TwitchIrcService : KittenWebSocketProvider, ITwitchIrcService
	{
		private const string TWITCH_IRC_ENDPOINT = "wss://irc-ws.chat.twitch.tv:443";

		private readonly ILogger _logger;
		private readonly ITwitchAuthService _twitchAuthService;

		private readonly char[] _ircMessageSeparator;

		public TwitchIrcService(ILogger logger, ITwitchAuthService twitchAuthService) : base(logger)
		{
			_logger = logger;
			_twitchAuthService = twitchAuthService;

			_ircMessageSeparator = new[] {'\r', '\n'};
		}

		public Task Start()
		{
			return Connect(TWITCH_IRC_ENDPOINT);
		}

		public Task Stop()
		{
			return Disconnect("Requested by service manager");
		}

		protected override void ReconnectHappenedHandler(ReconnectionInfo info)
		{
			_logger.Debug("(Re)connect happened - {Url} - {Type}", TWITCH_IRC_ENDPOINT, info.Type);

			SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");

			SendMessage($"PASS oauth:{_twitchAuthService.AccessToken}");
			SendMessage($"NICK {_twitchAuthService.LoggedInUser?.LoginName ?? "."}");
		}

		protected override void MessageReceivedHandler(ResponseMessage response)
		{
			var messages = response.Text.Split(_ircMessageSeparator, StringSplitOptions.RemoveEmptyEntries);

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

		protected override void DisconnectHappenedHandler(DisconnectionInfo info)
		{
			_logger.Information("Closed connection to Twitch IRC server");
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

				string? keyTmp = null;
				for (var curPos = 0; curPos < tagsAsSpan.Length; curPos++)
				{
					if (tagsAsSpan[curPos] == charSeparator)
					{
						if (charSeparator == ';')
						{
							tagsDictInternal[keyTmp!] = (curPos == startPos) ? string.Empty : tagsAsSpan.Slice(startPos, curPos - startPos - 1).ToString();

							charSeparator = '=';
							startPos = curPos + 1;
						}
						else
						{
							keyTmp = tagsAsSpan.Slice(startPos, curPos - startPos - 1).ToString();

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

		private void HandleParsedIrcMessage(ref ReadOnlyDictionary<string, string>? tags, ref string? prefix, ref string commandType, ref string? channelName, ref string? message)
		{
			switch (commandType)
			{
				case "PING":
					SendMessage("PONG :tmi.twitch.tv");
					break;
				case "376":
					break;
				case "NOTICE":
					break;
				case "USERNOTICE":
				case "PRIVMSG":
					break;
				case "JOIN":
				case "PART":
					break;
				case "ROOMSTATE":
					break;
				case "USERSTATE":
				case "GLOBALUSERSTATE":
					break;
				case "CLEARCHAT":
					break;
				case "CLEARMSG":
					break;
			}
		}
	}
}