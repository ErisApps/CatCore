using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Timers;
using Serilog;
using Websocket.Client;
using Websocket.Client.Logging;
using Websocket.Client.Models;

namespace CatCore.Services
{
	internal abstract class KittenWebSocketProvider
	{
		private const string DEFAULT_HEART_BEAT_MESSAGE = "ping";

		private readonly ILogger _logger;

		private WebsocketClient? _wss;

		private Timer? _heartBeatTimer;
		private string? _customHeartBeatMessage;

		private IDisposable? _reconnectHappenedSubscription;
		private IDisposable? _disconnectionHappenedSubscription;
		private IDisposable? _messageReceivedSubscription;

		protected KittenWebSocketProvider(ILogger logger)
		{
			_logger = logger;

			// Disable internal Websocket.Client logging
			LogProvider.IsDisabled = true;
		}

		public bool IsConnected => _wss?.IsRunning ?? false;

		protected async Task Connect(string uri, TimeSpan? heartBeatInterval = null, string? customHeartBeatMessage = null)
		{
			_customHeartBeatMessage = customHeartBeatMessage ?? DEFAULT_HEART_BEAT_MESSAGE;

			await Disconnect("Restarting websocket connection").ConfigureAwait(false);

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

		protected async Task Disconnect(string? reason = null)
		{
			_heartBeatTimer?.Stop();

			if (_wss?.IsStarted ?? false)
			{
				await _wss.Stop(WebSocketCloseStatus.NormalClosure, reason ?? "Closure was requested").ConfigureAwait(false);
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

		protected virtual void ReconnectHappenedHandler(ReconnectionInfo info)
		{
			if (_heartBeatTimer != null)
			{
				_heartBeatTimer.Stop();
				_heartBeatTimer.Start();
			}

			_logger.Debug("(Re)connect happened - {Url} - {Type}", _wss!.Url.AbsoluteUri, info.Type);
		}

		protected virtual void DisconnectHappenedHandler(DisconnectionInfo info)
		{
			_heartBeatTimer?.Stop();
		}

		protected virtual void MessageReceivedHandler(ResponseMessage response)
		{
			if (_heartBeatTimer != null)
			{
				_heartBeatTimer.Stop();
				_heartBeatTimer.Start();
			}
		}

		private void HeartBeatTimerOnElapsed(object sender, ElapsedEventArgs e)
		{
			SendMessage(_customHeartBeatMessage!);
		}
	}
}