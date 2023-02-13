using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Services.Interfaces;
using IWebsocketClientLite.PCL;
using Serilog;
using WebsocketClientLite.PCL;
using WebsocketClientLite.PCL.CustomException;

namespace CatCore.Services
{
	internal class KittenWebSocketProvider : IKittenWebSocketProvider
	{
		private readonly ILogger _logger;
		private readonly SemaphoreSlim _connectionLocker = new(1, 1);

		private TcpClient? _underlyingTcpClient;
		private MessageWebsocketRx? _websocketClient;

		private IDisposable? _disposableWebsocketSubscription;
		private Subject<(IDataframe? dataframe, ConnectionStatus state)>? _websocketConnectionSubject;
		private IDisposable? _connectionInitObservable;
		private IDisposable? _connectObservable;
		private IDisposable? _disconnectObservable;
		private IDisposable? _messageReceivedObservable;

		public event AsyncEventHandlerDefinitions.AsyncEventHandler<WebSocketConnection>? ConnectHappened;
		public event AsyncEventHandlerDefinitions.AsyncEventHandler? DisconnectHappened;
		public event AsyncEventHandlerDefinitions.AsyncEventHandler<WebSocketConnection, string>? MessageReceived;

		public bool IsConnected => _websocketClient?.IsConnected ?? false;

		public KittenWebSocketProvider(ILogger logger)
		{
			_logger = logger;
		}

		public async Task Connect(string url)
		{
			await Disconnect().ConfigureAwait(false);

			using var _ = await Synchronization.LockAsync(_connectionLocker).ConfigureAwait(false);
			var targetUri = new Uri(url);
			_underlyingTcpClient = CreateTcpClient(targetUri);
			_websocketClient = new MessageWebsocketRx(_underlyingTcpClient, hasTransferTcpSocketLifeCycleOwnership: true)
			{
				Headers = new Dictionary<string, string> { { "Pragma", "no-cache" }, { "Cache-Control", "no-cache" } }, TlsProtocolType = SslProtocols.Tls12
			};

			var tcs = new TaskCompletionSource<object>();

			var wrapper = new WebSocketConnection(_websocketClient, _logger);

			var websocketConnectionObservable = _websocketClient
				.WebsocketConnectWithStatusObservable(targetUri, handshakeTimeout: TimeSpan.FromSeconds(15))
				.ObserveOn(System.Reactive.Concurrency.ThreadPoolScheduler.Instance)
				.Catch<(IDataframe? dataframe, ConnectionStatus state), WebsocketClientLiteTcpConnectException>(ex =>
				{
					_logger.Error(ex, "A tcp connect exception occurred. Marking connection as failed");
					return Observable.Return<(IDataframe? dataframe, ConnectionStatus state)>((null, ConnectionStatus.ConnectionFailed));
				})
				.Catch<(IDataframe? dataframe, ConnectionStatus state), WebsocketClientLiteException>(ex =>
				{
					_logger.Error(ex, "A websocket error occurred. Marking connection as failed");
					return Observable.Return<(IDataframe? dataframe, ConnectionStatus state)>((null, ConnectionStatus.ConnectionFailed));
				});
			_websocketConnectionSubject = new Subject<(IDataframe? dataframe, ConnectionStatus state)>();

			_connectObservable = _websocketConnectionSubject
				.Where(tuple => tuple.state == ConnectionStatus.WebsocketConnected)
				.Do(_ => _logger.Debug("Connected to url: {Url}", url))
				.Select(_ => Observable.FromAsync(async () => await ConnectHandler(wrapper)))
				.Concat()
				.Subscribe();
			_connectionInitObservable = _websocketConnectionSubject
				.Where(tuple => tuple.state is ConnectionStatus.WebsocketConnected
					or ConnectionStatus.Disconnected
					or ConnectionStatus.ForcefullyDisconnected
					or ConnectionStatus.Aborted
					or ConnectionStatus.ConnectionFailed
					or ConnectionStatus.Close)
				.Do(_ => tcs.SetResult(null!))
				.Subscribe();
			_disconnectObservable = _websocketConnectionSubject
				.Where(tuple => tuple.state is ConnectionStatus.Disconnected
					or ConnectionStatus.ForcefullyDisconnected
					or ConnectionStatus.Aborted
					or ConnectionStatus.ConnectionFailed
					or ConnectionStatus.Close)
				.Do(tuple => _logger.Debug("A disconnect occured ({State}) for url: {Url}", tuple.state, url))
				.Select(_ => Observable.FromAsync(() => Connect(url)))
				.Concat()
				.Subscribe();
			_messageReceivedObservable = _websocketConnectionSubject
				.Where(tuple => tuple.state == ConnectionStatus.DataframeReceived && tuple.dataframe != null)
				.Select(tuple => Observable.FromAsync(() => MessageReceivedHandler(wrapper, tuple.dataframe!.Message!)))
				.Concat()
				.Subscribe();

			websocketConnectionObservable.Subscribe(_websocketConnectionSubject);

			await tcs.Task;
		}

		public async Task Disconnect(string? reason = null)
		{
			using var _ = await Synchronization.LockAsync(_connectionLocker).ConfigureAwait(false);
			if (_websocketClient == null)
			{
				return;
			}

			_logger.Warning("Executing disconnect logic. Optional reason: {Reason}", reason);

			_disposableWebsocketSubscription?.Dispose();
			_disposableWebsocketSubscription = null;

			_websocketConnectionSubject = null;

			_connectionInitObservable?.Dispose();
			_connectionInitObservable = null;

			_connectObservable?.Dispose();
			_connectObservable = null;

			_disconnectObservable?.Dispose();
			_disconnectObservable = null;

			_messageReceivedObservable?.Dispose();
			_messageReceivedObservable = null;

			_underlyingTcpClient = null;

			_websocketClient = null;

			await DisconnectHandler().ConfigureAwait(false);
		}

		private async Task ConnectHandler(WebSocketConnection webSocketConnection)
		{
			if (ConnectHappened != null)
			{
				await ConnectHappened.Invoke(webSocketConnection);
			}
		}

		private async Task DisconnectHandler()
		{
			if (DisconnectHappened != null)
			{
				await DisconnectHappened.Invoke();
			}
		}

		private async Task MessageReceivedHandler(WebSocketConnection webSocketConnection, string message)
		{
			if (MessageReceived != null)
			{
				await MessageReceived.Invoke(webSocketConnection, message);
			}
		}

		// ReSharper disable once UnusedParameter.Local
		private TcpClient CreateTcpClient(Uri destination)
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
		private Socket CreateProxiedSocket(Uri proxy, Uri destination)
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			socket.Connect(proxy.Host, proxy.Port);

			var connectMessage = System.Text.Encoding.UTF8.GetBytes($"CONNECT {destination.Host}:{destination.Port} HTTP/1.1{Environment.NewLine}{Environment.NewLine}");
			socket.Send(connectMessage);

			var receiveBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(512);
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

	internal class WebSocketConnection
	{
		private readonly ILogger _logger;
		private readonly MessageWebsocketRx _wss;

		public WebSocketConnection(MessageWebsocketRx websocketClient, ILogger logger)
		{
			_wss = websocketClient;
			_logger = logger;
		}

		private bool IsConnected => _wss.IsConnected;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SendMessageFireAndForget(string message)
		{
			_ = SendMessage(message);
		}

		public async Task SendMessage(string message)
		{
			if (IsConnected)
			{
				_logger.Verbose("Sending message");
				await _wss.GetSender().SendText(message).ConfigureAwait(false);
			}
			else
			{
				_logger.Warning("WS is closed, couldn't send message");
			}
		}
	}
}