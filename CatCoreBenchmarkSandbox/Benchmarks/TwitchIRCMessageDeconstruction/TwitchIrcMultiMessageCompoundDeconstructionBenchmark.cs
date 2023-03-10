using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;

namespace CatCoreBenchmarkSandbox.Benchmarks.TwitchIRCMessageDeconstruction
{
	[MemoryDiagnoser]
	[CategoriesColumn, AllStatisticsColumn, BaselineColumn, MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn, MedianColumn]
	public class TwitchIrcMultiMessageCompoundDeconstructionBenchmark
	{
		[Params(
			":tmi.twitch.tv 001 realeris :Welcome, GLHF!\r\n:tmi.twitch.tv 002 realeris :Your host is tmi.twitch.tv\r\n:tmi.twitch.tv 003 realeris :This server is rather new\r\n:tmi.twitch.tv 004 realeris :-\r\n:tmi.twitch.tv 375 realeris :-\r\n:tmi.twitch.tv 372 realeris :You are in a maze of twisty passages, all alike.\r\n:tmi.twitch.tv 376 realeris :>\r\n@badge-info=;badges=;color=#FF69B4;display-name=RealEris;emote-sets=0,237026,489998,300374282,303670737,477339272,592920959,610186276;user-id=405499635;user-type= :tmi.twitch.tv GLOBALUSERSTATE\r\n")]
		public string RawIrcMultiMessage = null!;

		private readonly char[] _ircMessageSeparator = { '\r', '\n' };

		[Benchmark(Baseline = true)]
		public void IntermediateStringSplitBenchmark()
		{
			var messages = RawIrcMultiMessage.Split(_ircMessageSeparator, StringSplitOptions.RemoveEmptyEntries);
			foreach (var messageInternal in messages)
			{
				// Handle IRC messages here
				IrcExtensions.Old.ParseIrcMessage(messageInternal, out var tags, out var prefix, out var commandType, out var channelName, out var message);
			}
		}

		[Benchmark]
		public void InlineSpanBasedSplitBenchmark()
		{
			var rawMessagesAsSpan = RawIrcMultiMessage.AsSpan();
			var endPosition = 0;
			do
			{
				var startPosition = endPosition;
				while (endPosition + 1 < rawMessagesAsSpan.Length)
				{
					if (rawMessagesAsSpan[endPosition] == '\r' && rawMessagesAsSpan[endPosition + 1] == '\n')
					{
						break;
					}

					endPosition++;
				}

				IrcExtensions.New.ParseIrcMessage(rawMessagesAsSpan.Slice(startPosition, endPosition - startPosition), out var tags, out var prefix, out var commandType, out var channelName,
					out var message);

				endPosition += 2;
			} while (endPosition < rawMessagesAsSpan.Length);
		}


		/// <summary>
		/// V1 had an issue where the last letter of non /r/n terminated messages would be dropped
		/// </summary>
		[Benchmark]
		public void InlineSpanBasedSplitBenchmarkV2()
		{
			void handleSingleMessage(ReadOnlySpan<char> singleMessageAsSpan)
			{
				IrcExtensions.New.ParseIrcMessage(singleMessageAsSpan, out var tags, out var prefix, out var commandType, out var channelName, out var message);
			}

			var rawMessagesAsSpan = RawIrcMultiMessage.AsSpan();
			while (true)
			{

				var messageSeparatorPosition = rawMessagesAsSpan.IndexOf("\r\n".AsSpan());
				if (messageSeparatorPosition == -1)
				{
					if (rawMessagesAsSpan.Length > 0)
					{
						handleSingleMessage(rawMessagesAsSpan);
					}

					break;
				}

				// Handle IRC messages here
				handleSingleMessage(rawMessagesAsSpan.Slice(0, messageSeparatorPosition));

				rawMessagesAsSpan = rawMessagesAsSpan.Slice(messageSeparatorPosition + 2);
			}
		}

		internal static class IrcExtensions
		{
			internal static class Old
			{
				// ReSharper disable once CognitiveComplexity
				internal static void ParseIrcMessage(string messageInternal, out ReadOnlyDictionary<string, string>? tags, out string? prefix, out string commandType, out string? channelName,
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
			}

			internal static class New
			{
				// ReSharper disable once CognitiveComplexity
				internal static void ParseIrcMessage(ReadOnlySpan<char> messageAsSpan, out ReadOnlyDictionary<string, string>? tags, out string? prefix, out string commandType,
					out string? channelName,
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
			}
		}
	}
}