using System;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Services.Interfaces;
using Serilog;
using Websocket.Client;
using Websocket.Client.Logging;
using Websocket.Client.Models;

namespace CatCore.Services
{
	internal sealed class KittenWebSocketProvider : IKittenWebSocketProvider
	{
		private readonly ILogger _logger;
		private readonly SemaphoreSlim _connectionLocker = new(1,1 );

		private WebsocketClient? _wss;

		private IDisposable? _reconnectHappenedSubscription;
		private IDisposable? _disconnectionHappenedSubscription;
		private IDisposable? _messageReceivedSubscription;

		public event Action? ConnectHappened;
		public event Action? DisconnectHappened;
		public event Action<string>? MessageReceived;

		public KittenWebSocketProvider(ILogger logger)
		{
			_logger = logger;

			// Disable internal Websocket.Client logging
			LogProvider.IsDisabled = true;
		}

		public bool IsConnected => _wss?.IsRunning ?? false;

		public async Task Connect(string uri)
		{
			await Disconnect("Restarting websocket connection").ConfigureAwait(false);

			using var _ = await Synchronization.LockAsync(_connectionLocker).ConfigureAwait(false);
			_wss = new WebsocketClient(new Uri(uri), () => new ClientWebSocket
			{
				Options =
				{
					KeepAliveInterval = TimeSpan.Zero,
#if !RELEASE
					Proxy = SharedProxyProvider.PROXY
#endif
				}
			})
			{
				ReconnectTimeout = TimeSpan.FromMinutes(10)
			};

			_reconnectHappenedSubscription = _wss.ReconnectionHappened.ObserveOn(System.Reactive.Concurrency.ThreadPoolScheduler.Instance).Subscribe(ReconnectHappenedHandler);
			_disconnectionHappenedSubscription = _wss.DisconnectionHappened.ObserveOn(System.Reactive.Concurrency.ThreadPoolScheduler.Instance).Subscribe(DisconnectHappenedHandler);
			_messageReceivedSubscription = _wss.MessageReceived.ObserveOn(System.Reactive.Concurrency.ThreadPoolScheduler.Instance).Subscribe(MessageReceivedHandler);

			await _wss.Start().ConfigureAwait(false);
		}

		public async Task Disconnect(string? reason = null)
		{
			using var _ = await Synchronization.LockAsync(_connectionLocker).ConfigureAwait(false);
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

		public async Task SendMessageInstant(string message)
		{
			if (IsConnected)
			{
				await _wss!.SendInstant(message).ConfigureAwait(false);
			}
		}

		private void ReconnectHappenedHandler(ReconnectionInfo info)
		{
			_logger.Debug("(Re)connect happened - Url: {Url} - Type: {Type}", _wss!.Url.ToString(), info.Type);

			ConnectHappened?.Invoke();
		}

		private void DisconnectHappenedHandler(DisconnectionInfo info)
		{
			DisconnectHappened?.Invoke();

			_logger.Warning("Closed connection to the server - Url: {Url} - Type: {Type}", _wss?.Url.ToString(), info.Type);
		}

		private void MessageReceivedHandler(ResponseMessage responseMessage)
		{
			MessageReceived?.Invoke(responseMessage.Text);
		}
	}
}