using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using CatCore.Services.Interfaces;
using Websocket.Client;
using Websocket.Client.Logging;
using Websocket.Client.Models;

namespace CatCore.Services
{
	internal class KittenWebSocketProvider : IKittenWebSocketProvider
	{
		private WebsocketClient? _wss;

		private IDisposable? _reconnectHappenedSubscription;
		private IDisposable? _disconnectionHappenedSubscription;
		private IDisposable? _messageReceivedSubscription;

		public event Action<ReconnectionInfo>? ReconnectHappened;
		public event Action<DisconnectionInfo>? DisconnectHappened;
		public event Action<ResponseMessage>? MessageReceived;

		public KittenWebSocketProvider()
		{
			// Disable internal Websocket.Client logging
			LogProvider.IsDisabled = true;
		}

		public bool IsConnected => _wss?.IsRunning ?? false;

		public async Task Connect(string uri, TimeSpan? heartBeatInterval = null, string? customHeartBeatMessage = null)
		{
			await Disconnect("Restarting websocket connection").ConfigureAwait(false);

			_wss = new WebsocketClient(new Uri(uri), () => new ClientWebSocket {Options = {KeepAliveInterval = TimeSpan.Zero, Proxy = new System.Net.WebProxy("192.168.0.126", 8888)}})
			{
				ReconnectTimeout = TimeSpan.FromMinutes(10)
			};
			_reconnectHappenedSubscription = _wss.ReconnectionHappened.Subscribe(ReconnectHappenedHandler);
			_disconnectionHappenedSubscription = _wss.DisconnectionHappened.Subscribe(DisconnectHappenedHandler);
			_messageReceivedSubscription = _wss.MessageReceived.Subscribe(MessageReceivedHandler);

			await _wss.StartOrFail().ConfigureAwait(false);
		}

		public async Task Disconnect(string? reason = null)
		{
			if (_wss?.IsStarted ?? false)
			{
				await _wss.Stop(WebSocketCloseStatus.NormalClosure, reason).ConfigureAwait(false);
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
			ReconnectHappened?.Invoke(info);
		}

		private void DisconnectHappenedHandler(DisconnectionInfo info)
		{
			DisconnectHappened?.Invoke(info);
		}

		private void MessageReceivedHandler(ResponseMessage message)
		{
			MessageReceived?.Invoke(message);
		}
	}
}