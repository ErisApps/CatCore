using System;
using System.Collections.Generic;
using System.Data;
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
		private static WebsocketClient? _twitchIrcWss;
		private static WebsocketClient? _twitchPubSubWss;
		private static Timer? _pingPongTimer;

		/// <remark>
		/// The websockets are currently utilising a web debugging proxy like Charles or Fiddler.
		/// Yeet the proxy assignment if you want to test stuff without such proxy.
		/// </remark>
		private static async Task Main(string[] args)
		{
			var accessToken = args.ElementAtOrDefault(0);
			if (accessToken == null)
			{
				throw new NoNullAllowedException(nameof(accessToken));
			}

			await TwitchIrcTesting(accessToken).ConfigureAwait(false);
			await TwitchPubSubTesting(accessToken).ConfigureAwait(false);

			while (Console.ReadKey().KeyChar != 'q')
			{
			}

			await _twitchIrcWss.StopOrFail(WebSocketCloseStatus.NormalClosure, "Requested by user");
			_twitchIrcWss.Dispose();

			await _twitchPubSubWss.StopOrFail(WebSocketCloseStatus.NormalClosure, "Requested by user");
			_twitchPubSubWss.Dispose();
		}

		private static async Task TwitchIrcTesting(string accessToken)
		{
			Console.WriteLine("Hello Twitch IRC!");

			_pingPongTimer = new Timer {Interval = 60 * 1000, Enabled = true, AutoReset = true,};
			var pubSubUrl = new Uri("wss://irc-ws.chat.twitch.tv:443");
			_twitchIrcWss = new WebsocketClient(pubSubUrl, () => new ClientWebSocket {Options = {Proxy = new System.Net.WebProxy("192.168.0.150", 8888), KeepAliveInterval = TimeSpan.FromMinutes(0)}})
			{
				ReconnectTimeout = TimeSpan.FromSeconds(330)
			};
			_twitchIrcWss.ReconnectionHappened.Subscribe(info =>
			{
				_pingPongTimer.Stop();
				_pingPongTimer.Start();
				Console.WriteLine($"(Re)connect happened - {info.Type:G}");
				_twitchIrcWss.Send($"PASS oauth:{accessToken}");
				_twitchIrcWss.Send("NICK realeris");
				_twitchIrcWss.Send("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");
				_twitchIrcWss.Send("JOIN #realeris");
			});
			_twitchIrcWss.DisconnectionHappened.Subscribe(info =>
			{
				Console.WriteLine($"{info.Type}");
				_pingPongTimer.Stop();
			});
			_twitchIrcWss.MessageReceived.Subscribe(wsMessage =>
			{
				_pingPongTimer.Stop();
				_pingPongTimer.Start();

				var messages = wsMessage.Text.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

				Console.WriteLine("=======================");
				foreach (var messageInternal in messages)
				{
					Console.WriteLine($"{wsMessage.MessageType:G} - {messageInternal}");
				}

				Console.WriteLine("=======================");
			});
			await _twitchIrcWss.StartOrFail().ConfigureAwait(false);
		}

		private static async Task TwitchPubSubTesting(string accessToken)
		{
			Console.WriteLine("Hello Twitch PubSub!");

			_pingPongTimer = new Timer {Interval = 60 * 1000, Enabled = true, AutoReset = true,};
			var pubSubUrl = new Uri("wss://pubsub-edge.twitch.tv");
			_twitchPubSubWss = new WebsocketClient(pubSubUrl,
				() => new ClientWebSocket {Options = {Proxy = new System.Net.WebProxy("192.168.0.150", 8888), KeepAliveInterval = TimeSpan.FromMinutes(10)}})
			{
				ReconnectTimeout = TimeSpan.FromSeconds(90)
			};
			_twitchPubSubWss.ReconnectionHappened.Subscribe(info =>
			{
				_pingPongTimer.Stop();
				_pingPongTimer.Start();
				Console.WriteLine($"(Re)connect happened - {info.Type:G}");
				_twitchPubSubWss.Send(JsonSerializer.Serialize(new ListenMessage
				{
					Nonce = "heya", Data = new ListenMessage.ListenMessageData {Topics = new List<string> {"channel-points-channel-v1.405499635"}, Token = accessToken}
				}));
			});
			_twitchPubSubWss.DisconnectionHappened.Subscribe(info =>
			{
				Console.WriteLine($"{info.Type}");
				_pingPongTimer.Stop();
			});
			_twitchPubSubWss.MessageReceived.Subscribe(message =>
			{
				_pingPongTimer.Stop();
				_pingPongTimer.Start();
				Console.WriteLine($"{message.MessageType:G} - {message.Text}");
			});
			await _twitchPubSubWss.StartOrFail().ConfigureAwait(false);

			_pingPongTimer.Elapsed += (_, _) => _twitchPubSubWss.Send(JsonSerializer.Serialize(new PingMessage()));
		}
	}
}