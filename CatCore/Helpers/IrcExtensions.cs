using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CatCore.Helpers
{
	internal static class IrcExtensions
	{
		// ReSharper disable once CognitiveComplexity
		internal static void ParseIrcMessage(ReadOnlySpan<char> messageAsSpan, out ReadOnlyDictionary<string, string>? tags, out string? prefix, out string commandType, out string? channelName,
			out string? message)
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
				int curPos;

				ReadOnlySpan<char> keyTmp = null;
				for (curPos = 0; curPos < tagsAsSpan.Length; curPos++)
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

				if (curPos != startPos)
				{
					tagsDictInternal[keyTmp.ToString()] = tagsAsSpan.Slice(startPos, curPos - startPos).ToString();
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
						var offset = messageAsSpan[0] == '#' ? 1 : 0;
						channelName = messageAsSpan.Slice(offset, position + 1 - offset).ToString();
					}

					break;
				}

				position++;
			}

			if (handledInLoop)
			{
				return;
			}

			if (messageAsSpan[0] == '#')
			{
				messageAsSpan = messageAsSpan.Slice(1);
			}

			channelName = messageAsSpan.ToString();
		}

		internal static bool ParsePrefix(this string? rawPrefix, out bool? isServer, out string? nickname, out string? username, out string? hostname)
		{
			isServer = null;
			nickname = null;
			username = null;
			hostname = null;

			if (string.IsNullOrEmpty(rawPrefix))
			{
				return false;
			}

			var rawPrefixSpan = rawPrefix.AsSpan();

			var usernamePosition = rawPrefixSpan.IndexOf('!') + 1;
			var hostnamePosition = rawPrefixSpan.IndexOf('@') + 1;

			if (usernamePosition == 1 || hostnamePosition == 1)
			{
				return false;
			}

			isServer = false;

			if (usernamePosition > 0)
			{
				nickname = rawPrefixSpan.Slice(0, usernamePosition - 1).ToString();
				if (hostnamePosition > 0)
				{
					username = rawPrefixSpan.Slice(usernamePosition, hostnamePosition - usernamePosition - 1).ToString();
					hostname = rawPrefixSpan.Slice(hostnamePosition).ToString();
				}
				else
				{
					username = rawPrefixSpan.Slice(usernamePosition).ToString();
				}
			}
			else if (hostnamePosition > 0)
			{
				nickname = rawPrefixSpan.Slice(0, hostnamePosition - 1).ToString();
				hostname = rawPrefixSpan.Slice(hostnamePosition).ToString();
			}
			else if (rawPrefixSpan.IndexOf('.') > 0)
			{
				hostname = rawPrefix;
				isServer = true;
			}
			else
			{
				nickname = rawPrefix;
			}

			return true;
		}
	}
}