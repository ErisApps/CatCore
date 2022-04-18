using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Services.Interfaces;
using IWebsocketClientLite.PCL;
using Serilog;
using WebsocketClientLite.PCL;

namespace CatCore.Services
{
	internal class KittenWebSocketProvider4 : IKittenWebSocketProvider
	{
		private readonly ILogger _logger;
		private readonly SemaphoreSlim _connectionLocker = new(1, 1);

		private TcpClient? _underlyingTcpClient;
		private MessageWebsocketRx? _websocketClient;

		private IDisposable? _disposableWebsocketSubscription;
		private IDisposable? _connectObservable;
		private IDisposable? _disconnectObservable;
		private IDisposable? _messageReceivedObservable;

		public event AsyncEventHandlerDefinitions.AsyncEventHandler<WebSocketConnection>? ConnectHappened;
		public event AsyncEventHandlerDefinitions.AsyncEventHandler? DisconnectHappened;
		public event AsyncEventHandlerDefinitions.AsyncEventHandler<WebSocketConnection, string>? MessageReceived;

		public bool IsConnected => _websocketClient?.IsConnected ?? false;

		public KittenWebSocketProvider4(ILogger logger)
		{
			_logger = logger;
		}

		public async Task Connect(string url)
		{
			await Disconnect().ConfigureAwait(false);

			using var _ = await Synchronization.LockAsync(_connectionLocker).ConfigureAwait(false);
			var targetUri = new Uri(url);
			_underlyingTcpClient = CreateTcpClient(targetUri);
			_websocketClient = new MessageWebsocketRx(_underlyingTcpClient)
			{
				Headers = new Dictionary<string, string> { { "Pragma", "no-cache" }, { "Cache-Control", "no-cache" } }, TlsProtocolType = SslProtocols.Tls12
			};

			var wrapper = new WebSocketConnection(_websocketClient);

			var websocketConnectionObservable = _websocketClient
				.WebsocketConnectWithStatusObservable(targetUri, timeout: TimeSpan.FromSeconds(15))
				.ObserveOn(System.Reactive.Concurrency.ThreadPoolScheduler.Instance);
			var websocketConnectionSubject = new Subject<(IDataframe? dataframe, ConnectionStatus state)>();

			_connectObservable = websocketConnectionSubject
				.Where(tuple => tuple.state == ConnectionStatus.WebsocketConnected)
				.Select(_ => Observable.FromAsync(async () => await ConnectHandler(wrapper)))
				.Concat()
				.Subscribe();
			_disconnectObservable = websocketConnectionSubject
				.Where(tuple => tuple.state is ConnectionStatus.Disconnected or ConnectionStatus.Aborted or ConnectionStatus.ConnectionFailed or ConnectionStatus.Close)
				.Do(tuple => _logger.Warning("An error occured"))
				.Select(_ => Observable.FromAsync(() => Connect(url)))
				.Concat()
				.Subscribe();
			_messageReceivedObservable = websocketConnectionSubject
				.Where(tuple => tuple.state == ConnectionStatus.DataframeReceived && tuple.dataframe != null)
				.Select(tuple => Observable.FromAsync(() => MessageReceivedHandler(wrapper, tuple.dataframe!.Message)))
				.Concat()
				.Subscribe();

			websocketConnectionObservable.Subscribe(websocketConnectionSubject);
		}

		public async Task Disconnect(string? reason = null)
		{
			using var _ = await Synchronization.LockAsync(_connectionLocker).ConfigureAwait(false);
			if (_websocketClient == null)
			{
				return;
			}

			_disposableWebsocketSubscription?.Dispose();
			_disposableWebsocketSubscription = null;

			_connectObservable?.Dispose();
			_connectObservable = null;

			_disconnectObservable?.Dispose();
			_disconnectObservable = null;

			_messageReceivedObservable?.Dispose();
			_messageReceivedObservable = null;

			_underlyingTcpClient?.Close();
			_underlyingTcpClient = null;

			_websocketClient = null;

			await DisconnectHandler().ConfigureAwait(false);
		}

		private async Task ConnectHandler(WebSocketConnection webSocketConnection)
		{
			_logger.Information(nameof(ConnectHandler));

			if (ConnectHappened != null)
			{
				await ConnectHappened.Invoke(webSocketConnection);
			}
		}

		private async Task DisconnectHandler()
		{
			_logger.Information(nameof(DisconnectHandler));

			if (DisconnectHappened != null)
			{
				await DisconnectHappened.Invoke();
			}
		}

		private async Task MessageReceivedHandler(WebSocketConnection webSocketConnection, string message)
		{
			_logger.Information(nameof(MessageReceivedHandler));

			if (MessageReceived != null)
			{
				await MessageReceived.Invoke(webSocketConnection, message);
			}
		}

		private static TcpClient CreateTcpClient(Uri destination)
		{
			var lingerOptions = new LingerOption(false, 0);
#if !RELEASE
			if (SharedProxyProvider.PROXY != null)
			{
				var proxiedSocket = CreateProxiedSocket(SharedProxyProvider.PROXY.Address, destination);
				return new TcpClient { LingerState = lingerOptions, Client = proxiedSocket };
			}
#endif

			return new TcpClient { LingerState = lingerOptions };
		}

#if !RELEASE
		private static Socket CreateProxiedSocket(Uri proxy, Uri destination)
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			socket.Connect(proxy.Host, proxy.Port);

			var connectMessage = System.Text.Encoding.UTF8.GetBytes($"CONNECT {destination.Host}:{destination.Port} HTTP/1.1{Environment.NewLine}{Environment.NewLine}");
			socket.Send(connectMessage);

			var receiveBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(128);
			var received = socket.Receive(receiveBuffer);

			var response = System.Text.Encoding.ASCII.GetString(receiveBuffer, 0, received);
			System.Buffers.ArrayPool<byte>.Shared.Return(receiveBuffer);

			if (!response.Contains("200"))
			{
				throw new Exception($"Error connecting to proxy server {destination.Host}:{destination.Port}. Response: {response}");
			}

			return socket;
		}
#endif
	}
}