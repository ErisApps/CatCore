using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Timers;
using CatCore.Services.Interfaces;
using Serilog;
using Websocket.Client;
using Websocket.Client.Models;

namespace CatCore.Services
{
	internal class KittenWebSocketProvider : IKittenWebSocketProvider
	{
		private const string DEFAULT_HEART_BEAT_MESSAGE = "ping";

		private readonly ILogger _logger;

		private WebsocketClient? _wss;

		private Timer? _heartBeatTimer;
		private string? _customHeartBeatMessage;

		private IDisposable? _reconnectHappenedSubscription;
		private IDisposable? _disconnectionHappenedSubscription;
		private IDisposable? _messageReceivedSubscription;

		public KittenWebSocketProvider(ILogger logger)
		{
			_logger = logger;
		}

		public bool IsConnected => _wss?.IsRunning ?? false;

		public event Action? OnOpen;
		public event Action? OnClose;
		public event Action<string>? OnMessageReceived;

		public async Task Connect(string uri, TimeSpan? heartBeatInterval = null, string? customHeartBeatMessage = null)
		{
			_customHeartBeatMessage = customHeartBeatMessage ?? DEFAULT_HEART_BEAT_MESSAGE;

			await Disconnect().ConfigureAwait(false);

			_wss = new WebsocketClient(new Uri(uri), () => new ClientWebSocket {Options = {KeepAliveInterval = TimeSpan.Zero}});
			_reconnectHappenedSubscription = _wss.ReconnectionHappened.Subscribe(ReconnectHappenedHandler);
			_disconnectionHappenedSubscription = _wss.DisconnectionHappened.Subscribe(DisconnectHappenedHandler);
			_messageReceivedSubscription = _wss.MessageReceived.Subscribe(MessageReceivedHandler);

			await _wss.StartOrFail().ConfigureAwait(false);

			if (heartBeatInterval != null)
			{
				_heartBeatTimer = new Timer(heartBeatInterval.Value.TotalMilliseconds);
				_heartBeatTimer.Start();
				_heartBeatTimer.Elapsed += HeartBeatTimerOnElapsed;
			}
		}

		public async Task Disconnect()
		{
			_heartBeatTimer?.Stop();

			if (_wss?.IsStarted ?? false)
			{
				// TODO: Add WebSocket closure reason
				await _wss.Stop(WebSocketCloseStatus.NormalClosure, string.Empty).ConfigureAwait(false);
				_reconnectHappenedSubscription?.Dispose();
				_disconnectionHappenedSubscription?.Dispose();
				_messageReceivedSubscription?.Dispose();
				_wss?.Dispose();
				_wss = null;
			}
		}

		public void SendMessage(string message)
		{
			if (IsConnected)
			{
				_wss!.Send(message);
			}
		}

		private void ReconnectHappenedHandler(ReconnectionInfo info)
		{
			_heartBeatTimer?.Stop();
			_heartBeatTimer?.Start();
			_logger.Debug("(Re)connect happened - {Url} - {Type}", _wss!.Url.AbsoluteUri, info.Type);

			OnOpen?.Invoke();
		}

		private void DisconnectHappenedHandler(DisconnectionInfo info)
		{
			_heartBeatTimer?.Stop();

			OnClose?.Invoke();
		}

		private void MessageReceivedHandler(ResponseMessage message)
		{
			_heartBeatTimer?.Stop();
			_heartBeatTimer?.Start();

			OnMessageReceived?.Invoke(message.Text);
		}

		private void HeartBeatTimerOnElapsed(object sender, ElapsedEventArgs e)
		{
			SendMessage(_customHeartBeatMessage!);
		}
	}
}