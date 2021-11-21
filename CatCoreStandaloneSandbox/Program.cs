using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using CatCoreStandaloneSandbox.Models.Messages;
using Websocket.Client;

namespace CatCoreStandaloneSandbox
{
	internal static class Program
	{
		private static WebsocketClient? _twitchPubSubWss;
		private static Timer? _pingPongTimer;

		/// <remark>
		/// The websockets might be utilising a web debugging proxy like Charles or Fiddler.
		/// Yeet the proxy assignment if you want to test stuff without such proxy.
		/// </remark>
		private static async Task Main(string[] args)
		{
			/*var accessToken = args.ElementAtOrDefault(0);
			if (accessToken == null)
			{
				throw new NoNullAllowedException(nameof(accessToken));
			}

			await TwitchPubSubTesting(accessToken).ConfigureAwait(false);

			while (Console.ReadKey().KeyChar != 'q')
			{
			}

			if (_twitchPubSubWss != null)
			{
				await _twitchPubSubWss.StopOrFail(WebSocketCloseStatus.NormalClosure, "Requested by user");
				_twitchPubSubWss.Dispose();
				_twitchPubSubWss = null;
			}*/

			var stoppyWatch = new Stopwatch();
			stoppyWatch.Start();

			EmojiReferenceReadingTesting4();
			// EmojiTesting();

			stoppyWatch.Stop();
			Console.WriteLine($"Processing took {stoppyWatch.Elapsed:c}");
		}

		private static async Task TwitchPubSubTesting(string accessToken)
		{
			Console.WriteLine("Hello Twitch PubSub!");

			_pingPongTimer = new Timer { Interval = 30 * 1000, Enabled = true, AutoReset = true, };
			var pubSubUrl = new Uri("wss://pubsub-edge.twitch.tv");
			_twitchPubSubWss = new WebsocketClient(pubSubUrl,
				() => new ClientWebSocket { Options = { Proxy = new System.Net.WebProxy("192.168.0.125", 8888), KeepAliveInterval = TimeSpan.Zero } }) { ReconnectTimeout = TimeSpan.FromSeconds(90) };

			const string VALID_NONCE_CHARS = "abcdefghijklmnopqrstuvwxyz0123456789";
			var random = new Random();

			string GenerateNonce()
			{
				var stringChars = new char[16];
				for (var i = 0; i < stringChars.Length; i++)
				{
					stringChars[i] = VALID_NONCE_CHARS[random.Next(VALID_NONCE_CHARS.Length)];
				}

				return new string(stringChars);
			}

			void SendListenTopicPubSubMessage(WebsocketClient pubSubWss, string topic, string mode = "LISTEN")
			{
				var message = JsonSerializer.Serialize(new TopicMessage(mode)
				{
					Nonce = GenerateNonce(), Data = new TopicMessage.TopicMessageData { Topics = new List<string> { topic }, Token = accessToken }
				});
				Console.WriteLine($"<<< {message}");

				pubSubWss.Send(message);
			}

			_twitchPubSubWss.ReconnectionHappened.Subscribe(async info =>
			{
				_pingPongTimer.Stop();
				_pingPongTimer.Start();
				Console.WriteLine($"(Re)connect happened - {info.Type:G}");

				SendListenTopicPubSubMessage(_twitchPubSubWss, "channel-bits-events-v2.405499635");
				SendListenTopicPubSubMessage(_twitchPubSubWss, "channel-bits-badge-unlocks.405499635");
				SendListenTopicPubSubMessage(_twitchPubSubWss, "channel-points-channel-v1.405499635");
				SendListenTopicPubSubMessage(_twitchPubSubWss, "channel-subscribe-events-v1.405499635");

				SendListenTopicPubSubMessage(_twitchPubSubWss, "chat_moderator_actions.405499635.405499635");
				SendListenTopicPubSubMessage(_twitchPubSubWss, "automod-queue.405499635.405499635");
				SendListenTopicPubSubMessage(_twitchPubSubWss, "user-moderation-notifications.405499635.405499635");

				SendListenTopicPubSubMessage(_twitchPubSubWss, "chatrooms-user-v1.405499635");

				SendListenTopicPubSubMessage(_twitchPubSubWss, "polls.405499635");
				SendListenTopicPubSubMessage(_twitchPubSubWss, "predictions-channel-v1.405499635");
			});
			_twitchPubSubWss.DisconnectionHappened.Subscribe(info =>
			{
				Console.WriteLine($"{info.Type}");
				_pingPongTimer.Stop();
			});
			_twitchPubSubWss.MessageReceived.Subscribe(receivedMessagePacket =>
			{
				_pingPongTimer.Stop();
				_pingPongTimer.Start();

				var jsonDocument = JsonDocument.Parse(receivedMessagePacket.Text);
				var rootElement = jsonDocument.RootElement;

				var type = rootElement.GetProperty("type").GetString();

				switch (type)
				{
					case "MESSAGE":
					{
						// TODO: Message handling code comes here
						var data = rootElement.GetProperty("data");

						var topicSpan = data.GetProperty("topic").GetString()!.AsSpan();
						var firstDotSeparator = topicSpan.IndexOf('.');
						var topic = topicSpan.Slice(0, firstDotSeparator).ToString();
						var message = data.GetProperty("message").GetString()!;

						Console.WriteLine($">>> {topic} => {message}");

						break;
					}
					default:
						Console.WriteLine($">>> {receivedMessagePacket.Text}");
						break;
				}
			});
			await _twitchPubSubWss.StartOrFail().ConfigureAwait(false);

			_pingPongTimer.Elapsed += (_, _) => _twitchPubSubWss.Send(JsonSerializer.Serialize(new PingMessage()));
		}

		private enum Status
		{
			Component,
			FullyQualified,
			MinimallyQualified,
			Unqualified
		}

		private static void EmojiReferenceReadingTesting()
		{
			var fullyQualifiedEmotes = File.ReadLines(Path.Combine(Environment.CurrentDirectory, "Resources", "emoji-test.txt"))
				.Where(line => !string.IsNullOrWhiteSpace(line) && line[0] != '#')
				.Select(line =>
				{
					var splitEntries = line.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

					var splitEntriesIndexCursor = splitEntries.IndexOf(";");
					var codepointsRepresentation = splitEntries.Take(splitEntriesIndexCursor).ToArray();

					var status = Enum.Parse<Status>(splitEntries[++splitEntriesIndexCursor].Replace("-", string.Empty), true);

					var emojiRepresentation = splitEntries[splitEntriesIndexCursor += 2];
					var unicodeVersionIntroduced = splitEntries[++splitEntriesIndexCursor];

					var emoteDescription = string.Join(" ", splitEntries.Skip(++splitEntriesIndexCursor));

					return new object[] { line, codepointsRepresentation, status, emojiRepresentation, unicodeVersionIntroduced, emoteDescription };
				})
				.Where(emojiData => ((Status) emojiData[2]) == Status.FullyQualified)
				.ToList();

			var groupedCodepointLists = fullyQualifiedEmotes
				.GroupBy(emojiData => ((string[]) emojiData[1]).Length)
				.OrderBy(x => x.Key)
				.ToDictionary(
					kvp => kvp.Key,
					grouping => grouping.OrderBy(x => string.Join("-", x[1])).ToList());

			Console.WriteLine(groupedCodepointLists.Count);

			var sortedCodepointsList = fullyQualifiedEmotes.OrderBy(x => string.Join("-", x[1])).ToList();

			var currentReferenceCodepoint = ((string[]) sortedCodepointsList[0][1])[0];
			var overlapDictionary = new Dictionary<string, List<object[]>>();
			for (var i = 1; i < sortedCodepointsList.Count; i++)
			{
				var oldReferenceCodepoint = currentReferenceCodepoint;
				currentReferenceCodepoint = ((string[]) sortedCodepointsList[i][1])[0];

				if (oldReferenceCodepoint.Equals(currentReferenceCodepoint, StringComparison.InvariantCultureIgnoreCase))
				{
					Console.WriteLine("Detected overlap in first codepoint");

					if (overlapDictionary.TryGetValue(currentReferenceCodepoint, out var overlapList))
					{
						overlapList.Add(sortedCodepointsList[i]);
					}
					else
					{
						overlapDictionary.TryAdd(currentReferenceCodepoint, new List<object[]> { sortedCodepointsList[i - 1], sortedCodepointsList[i] });
					}
				}
			}

			Console.WriteLine(fullyQualifiedEmotes.Count);
			Console.WriteLine();
		}

		private static void EmojiReferenceReadingTesting2()
		{
			var fullyQualifiedEmotes = File.ReadLines(Path.Combine(Environment.CurrentDirectory, "Resources", "emoji-test.txt"))
				.Where(line => !string.IsNullOrWhiteSpace(line) && line[0] != '#')
				.Skip(2000)
				.Select(line =>
				{
					var splitEntries = line.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

					var splitEntriesIndexCursor = splitEntries.IndexOf(";");
					var codepointsRepresentation = splitEntries.Take(splitEntriesIndexCursor).ToArray();

					var status = Enum.Parse<Status>(splitEntries[++splitEntriesIndexCursor].Replace("-", string.Empty), true);

					var emojiRepresentation = splitEntries[splitEntriesIndexCursor += 2];
					var emojiCharRepresentation = emojiRepresentation.ToArray();
					var unicodeVersionIntroduced = splitEntries[++splitEntriesIndexCursor];

					var emoteDescription = string.Join(" ", splitEntries.Skip(++splitEntriesIndexCursor));

					return new object[] { line, codepointsRepresentation, status, emojiRepresentation, emojiCharRepresentation, unicodeVersionIntroduced, emoteDescription };
				})
				.Where(emojiData => ((Status) emojiData[2]) == Status.FullyQualified)
				.ToList();

			var sortedCodepointsList = fullyQualifiedEmotes.OrderBy(x => string.Join("-", x[4])).ToList();

			var currentReferenceCodepoint = ((char[]) sortedCodepointsList[0][4])[0];
			var overlapDictionary = new Dictionary<char, List<object[]>>();
			for (var i = 1; i < sortedCodepointsList.Count; i++)
			{
				var oldReferenceCodepoint = currentReferenceCodepoint;
				currentReferenceCodepoint = ((char[]) sortedCodepointsList[i][4])[0];

				if (oldReferenceCodepoint.Equals(currentReferenceCodepoint))
				{
					Console.WriteLine("Detected overlap in first codepoint");

					if (overlapDictionary.TryGetValue(currentReferenceCodepoint, out var overlapList))
					{
						overlapList.Add(sortedCodepointsList[i]);
					}
					else
					{
						overlapDictionary.TryAdd(currentReferenceCodepoint, new List<object[]> { sortedCodepointsList[i - 1], sortedCodepointsList[i] });
					}
				}
			}

			Console.WriteLine(fullyQualifiedEmotes.Count);
			Console.WriteLine();
		}

		private static void EmojiReferenceReadingTesting3()
		{
			var fullyQualifiedEmotes = File.ReadLines(Path.Combine(Environment.CurrentDirectory, "Resources", "emoji-test.txt"))
				.Where(line => !string.IsNullOrWhiteSpace(line) && line[0] != '#')
				.Select(line =>
				{
					var splitEntries = line.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

					var splitEntriesIndexCursor = splitEntries.IndexOf(";");
					var codepointsRepresentation = splitEntries.Take(splitEntriesIndexCursor).ToArray();

					var status = Enum.Parse<Status>(splitEntries[++splitEntriesIndexCursor].Replace("-", string.Empty), true);

					var emojiRepresentation = splitEntries[splitEntriesIndexCursor += 2];
					var emojiCharRepresentation = emojiRepresentation.ToArray();
					var unicodeVersionIntroduced = splitEntries[++splitEntriesIndexCursor];

					var emoteDescription = string.Join(" ", splitEntries.Skip(++splitEntriesIndexCursor));

					return new object[] { line, codepointsRepresentation, status, emojiRepresentation, emojiCharRepresentation, unicodeVersionIntroduced, emoteDescription };
				})
				.Where(emojiData => ((Status) emojiData[2]) == Status.FullyQualified)
				.ToList();

			var referenceDictionary = new Dictionary<char, List<object[]>>();
			foreach (var t in fullyQualifiedEmotes)
			{
				var currentReferenceCodepoint = ((char[]) t[4])[0];
				if (referenceDictionary.TryGetValue(currentReferenceCodepoint, out var referenceList))
				{
					referenceList.Add(t);
				}
				else
				{
					referenceDictionary.TryAdd(currentReferenceCodepoint, new List<object[]> { t });
				}
			}

			Console.WriteLine(fullyQualifiedEmotes.Count);
			Console.WriteLine();

			var testEntries = new[] { "😸", "I 🧡 Twemoji! 🥳", "I've eaten Chinese food 😱😍🍱🍣🍥🍙🍘🍚🍜🍱🍣🍥🍙🍘🍚🍜", "🧝‍♀️", "🏳️‍⚧️" };

			foreach (var entry in testEntries)
			{
				Console.WriteLine(entry);
				for (var i = 0; i < entry.Length; i++)
				{
					if (referenceDictionary.TryGetValue(entry[i], out var possibleEmotes))
					{
						Console.WriteLine($"Possibly emote at index {i} (1 of {possibleEmotes.Count})");
					}
				}

				Console.WriteLine();
			}
		}

		private static void EmojiReferenceReadingTesting4()
		{
			var fullyQualifiedEmotes = File.ReadLines(Path.Combine(Environment.CurrentDirectory, "Resources", "Unicode13_1EmojiTest.txt"))
				.Where(line => !string.IsNullOrWhiteSpace(line) && line[0] != '#')
				.Select(line =>
				{
					var splitEntries = line.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

					var splitEntriesIndexCursor = splitEntries.IndexOf(";");
					var codepointsRepresentation = splitEntries.Take(splitEntriesIndexCursor).ToArray();

					var status = Enum.Parse<Status>(splitEntries[++splitEntriesIndexCursor].Replace("-", string.Empty), true);

					var emojiRepresentation = splitEntries[splitEntriesIndexCursor += 2];
					var emojiCharRepresentation = emojiRepresentation.ToArray();
					var unicodeVersionIntroduced = splitEntries[++splitEntriesIndexCursor];

					var emoteDescription = string.Join(" ", splitEntries.Skip(++splitEntriesIndexCursor));

					return new object[] { line, codepointsRepresentation, status, emojiRepresentation, emojiCharRepresentation, unicodeVersionIntroduced, emoteDescription };
				})
				.Where(emojiData => ((Status) emojiData[2]) == Status.FullyQualified)
				.ToList();


			var referenceDictionary = new EmojiTreeRoot();
			for (var i = 0; i < fullyQualifiedEmotes.Count; i++)
			{
				var emoteEntry = fullyQualifiedEmotes[i];
				var codepoints = ((char[]) emoteEntry[4]);
				referenceDictionary.AddToTree(emoteEntry, codepoints);
			}

			Console.WriteLine(fullyQualifiedEmotes.Count);
			Console.WriteLine();

			var testEntries = new[] { "😸", "I 🧡 Twemoji! 🥳", "I've eaten Chinese food 😱😍🍱🍣🍥🍙🍘🍚🍜🍱🍣🍥🍙🍘🍚🍜", "🧝‍♀️", "🏳️‍⚧️", "🏳️‍⚧️rights are human rights" };

			foreach (var entry in testEntries)
			{
				Console.WriteLine($"{entry} (Length: {entry.Length})");
				for (var i = 0; i < entry.Length; i++)
				{
					var emojiTreeLeaf = referenceDictionary.LookupLeaf(entry, i);
					if (emojiTreeLeaf != null)
					{
						Console.WriteLine($"Found emote between indexes [{i}-{i + emojiTreeLeaf.Depth}] (Length: {emojiTreeLeaf.Depth + 1})\n" +
						                  $"  Twemoji url: {emojiTreeLeaf.Url}\n" +
						                  $"  Description: {emojiTreeLeaf.RawObject[6]}");
						i = (int) (i + emojiTreeLeaf.Depth);
					}
				}

				Console.WriteLine();
			}
		}

		private static void EmojiTesting()
		{
			var testEntries = new[] { "😸", "I 🧡 Twemoji! 🥳", "I've eaten Chinese food 😱😍🍱🍣🍥🍙🍘🍚🍜🍱🍣🍥🍙🍘🍚🍜", "🧝‍♀️", "🏳️‍⚧️rights are human rights" };

			foreach (var entry in testEntries)
			{
				// Console.WriteLine(entry);

				for (var i = 0; i < entry.Length; i++)
				{
					var codepoint = char.IsHighSurrogate(entry[i])
						? char.ConvertToUtf32(entry, i++)
						: entry[i];

					// Console.WriteLine($"{codepoint:X} - {CharUnicodeInfo.GetUnicodeCategory(codepoint)}");
				}

				// Console.WriteLine();
			}
		}

		private static void CustomEmoteMetadataFetchingTesting()
		{
			// BTTV
			// https://api.betterttv.net/3/cached/emotes/global
			// https://api.betterttv.net/3/cached/users/twitch/${channel.id}
			// https://api.betterttv.net/3/cached/frankerfacez/emotes/global
			// https://api.betterttv.net/3/cached/frankerfacez/users/twitch/${currentChannel.id}
			// https://api.betterttv.net/3/cached/changelog
			// https://api.betterttv.net/3/cached/badges
			// https://api.betterttv.net/3/
			// https://api.betterttv.net/3/
			// https://api.betterttv.net/3/

			// FrankerFacez
			// https://api.frankerfacez.com/docs/
			// https://api.frankerfacez.com/v1/set/global
			// https://api.frankerfacez.com/v1/room/id/${channelId}

			// SevenTV (7TV)
			// https://api.7tv.app/v2/emotes/global
			// https://api.7tv.app/v2/users/${channelLogin}/emotes

			// Version can be 1x, 2x or 3x
			/*emoteUrl(emoteId, version = '3x') {
			 	// return `http://cdn.betterttv.net/emote/54fa8f1401e468494b85b537/3x`;
			 	return `http://cdn.betterttv.net/emote/${emoteId}/${version}`;
			},*/
		}

		private abstract class EmojiTreeNodeBase : Dictionary<char, IEmojiNode>
		{
			public void AddToTree(object[] rawObject, char[] codepoints, uint depth = 0)
			{
				var key = codepoints[depth];
				if (TryGetValue(key, out var node))
				{
					if (codepoints.Length - 1 == depth)
					{
						if (node is EmojiTreeNodeBlock block)
						{
							block.RawObject = rawObject;
						}
					}
					else
					{
						EmojiTreeNodeBlock block;
						if (node is EmojiTreeLeaf leaf)
						{
							block = leaf.UpgradeToBlock();
							this[key] = block;
						}
						else
						{
							block = (EmojiTreeNodeBlock) node;
						}

						block.AddToTree(rawObject, codepoints, ++depth);
					}
				}
				else
				{
					IEmojiNode newNode;
					if (codepoints.Length - 1 == depth)
					{
						newNode = new EmojiTreeLeaf { Key = key, Depth = depth, RawObject = rawObject };
					}
					else
					{
						newNode = new EmojiTreeNodeBlock { Key = key, Depth = depth };
						((EmojiTreeNodeBlock) newNode).AddToTree(rawObject, codepoints, ++depth);
					}

					TryAdd(key, newNode);
				}
			}

			public IEmojiTreeLeaf? LookupLeaf(string findNextEmote, int startPos)
			{
				if (TryGetValue(findNextEmote[startPos], out var node))
				{
					if (node is EmojiTreeNodeBlock block)
					{
						var possibleLeaf = block.LookupLeaf(findNextEmote, ++startPos);
						if (possibleLeaf == null && block.RawObject != null)
						{
							return block;
						}

						return possibleLeaf;
					}

					if (node is EmojiTreeLeaf leaf)
					{
						return leaf;
					}
				}

				return null;
			}
		}

		private class EmojiTreeRoot : EmojiTreeNodeBase
		{
		}

		private interface IEmojiNode
		{
			char Key { get; init; }
			uint Depth { get; init; }
		}

		private interface IEmojiTreeLeaf : IEmojiNode
		{
			object[] RawObject { get; }
			string Url => $"https://twemoji.maxcdn.com/v/latest/72x72/{string.Join("-", (string[]) RawObject[1]).ToLowerInvariant()}.png";
		}

		private class EmojiTreeNodeBlock : EmojiTreeNodeBase, IEmojiTreeLeaf
		{
			public char Key { get; init; }
			public uint Depth { get; init; }

			public object[]? RawObject { get; set; }
			public string Url => RawObject != null ? $"https://twemoji.maxcdn.com/v/latest/72x72/{string.Join("-", (string[]) RawObject[1]).ToLowerInvariant()}.png" : string.Empty;

			public override string ToString()
			{
				return $"Object is a node block with {Count} branches, it is {(RawObject == null ? "NOT" : string.Empty)} a leaf node{(RawObject == null ? string.Empty : " as well")}...";
			}
		}

		private class EmojiTreeLeaf : IEmojiTreeLeaf
		{
			public char Key { get; init; }
			public uint Depth { get; init; }

			public object[] RawObject { get; init; } = null!;
			public string Url => $"https://twemoji.maxcdn.com/v/latest/72x72/{string.Join("-", (string[]) RawObject[1]).ToLowerInvariant()}.png";

			public EmojiTreeNodeBlock UpgradeToBlock()
			{
				return new EmojiTreeNodeBlock { Key = Key, Depth = Depth, RawObject = RawObject };
			}

			public override string ToString()
			{
				return $"Object is a leaf with target url {Url}";
			}
		}
	}
}