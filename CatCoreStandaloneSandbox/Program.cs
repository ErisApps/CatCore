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

			// EmojiReferenceReadingTesting();
			EmojiTesting();

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

		private static void EmojiTesting()
		{
			var testEntries = new[] { "😸", "I 🧡 Twemoji! 🥳", "I've eaten Chinese food 😱😍🍱🍣🍥🍙🍘🍚🍜🍱🍣🍥🍙🍘🍚🍜", "🧝‍♀️", "🏳️‍⚧️" };

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

		private static async Task CustomEmoteMetadataFetchingTesting()
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
	}
}