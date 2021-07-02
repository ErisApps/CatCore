using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Services.Interfaces;
using CatCore.Services.Sockets;
using Serilog;

namespace CatCore.Services
{
	// Just copied sample code for socket server
	internal class KittenRawSocketProvider : IKittenRawSocketProvider
	{
		private const int SOCKET_PORT = 8338;

		// Not sure if concurrent is needed here
		private readonly ConcurrentDictionary<Guid, ClientSocket> _connectedClients = new ConcurrentDictionary<Guid, ClientSocket>();

		private static SemaphoreSlim NewSemaphore()
		{
			return new SemaphoreSlim(0, 1);
		}

		// Thread signal.
		private SemaphoreSlim _allDone = NewSemaphore();
		private readonly ILogger _logger;

		private bool _isServerRunning;

		internal CancellationTokenSource? ServerCts {  get; private set; }

#pragma warning disable 649
		public event Action<ClientSocket>? OnConnect;
		public event Action<ClientSocket, ReceivedData>? OnReceive;
		public event Action<ClientSocket>? OnDisconnect;
#pragma warning restore 649

		public KittenRawSocketProvider(ILogger logger)
		{
			_logger = logger;
		}

		private bool ValidateServerNotRunning()
		{
			return !_isServerRunning && !ServerCts?.IsCancellationRequested == null;
		}

		private async void StartListening(CancellationTokenSource cts)
		{
			ServerCts = cts;

			// Establish the local endpoint for the socket.
			IPAddress ipAddress = IPAddress.Any;
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, SOCKET_PORT);

			// Create a TCP/IP socket.
			Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			// Bind the socket to the local endpoint and listen for incoming connections.
			try
			{
				_logger.Information($"Binding to port {localEndPoint.Address.MapToIPv4()}:{localEndPoint.Port}");
				listener.Bind(localEndPoint);
				listener.Listen(20); //back log is amount of clients allowed to wait

				// Set the event to nonsignaled state.
				_allDone = NewSemaphore();

				_isServerRunning = true;
				while (!ServerCts.IsCancellationRequested)
				{
					// Start an asynchronous socket to listen for connections.
					_logger.Information("Waiting for a connection...");
					listener.BeginAccept(
						AcceptCallback,
						listener);


					// Wait until a connection is made before continuing.
					// this avoids eating CPU cycles
					await _allDone.WaitAsync(ServerCts.Token).ConfigureAwait(false);
				}

			}
			catch (Exception e)
			{
				_logger.Fatal(e.Message, e,ToString());
			}

			_isServerRunning = false;
		}

		private void AcceptCallback(IAsyncResult ar)
		{
			// Signal the main thread to continue.
			_allDone.Release();

			if (ServerCts is null || ServerCts.IsCancellationRequested)
			{
				return;
			}

			// Get the socket that handles the client request.
			Socket listener = (Socket) ar.AsyncState;
			Socket handler = listener.EndAccept(ar);

			var guid = Guid.NewGuid();

			// Never have a duplicate
			while (_connectedClients.ContainsKey(guid))
			{
				guid = Guid.NewGuid();
			}

			ClientSocket clientSocket = new ClientSocket(handler, guid, ServerCts!, HandleDisconnect, HandleRead);
			_connectedClients[guid] = clientSocket;

			OnConnect?.Invoke(clientSocket);
		}

		private void HandleRead(ClientSocket clientSocket, ReceivedData receivedData)
		{
			OnReceive?.Invoke(clientSocket, receivedData);
		}


		private void HandleDisconnect(ClientSocket clientSocket)
		{
			var socket = clientSocket.WorkSocket;

			if (socket.Connected)
			{
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}

			_connectedClients.TryRemove(clientSocket.Uuid, out _);

			OnDisconnect?.Invoke(clientSocket);
		}

		public void Initialize()
		{
			if (!ValidateServerNotRunning())
			{
				_logger.Warning("(This can be ignored if intentional) The server is still running, what is wrong with you? The poor kitty can't handle two socket servers! ;-;");
				return;
			}

			_logger.Information("Starting socket server");

			ServerCts = new CancellationTokenSource();
			Task.Run(() =>
			{
				try
				{
					StartListening(ServerCts);
				}
				catch (Exception e)
				{
					_logger.Error(e.Message, e.ToString());
				}
			});
		}

		public void Dispose()
		{

			ServerCts!.Dispose();
		}

		public bool isServerRunning()
		{
			return _isServerRunning;
		}
	}
}