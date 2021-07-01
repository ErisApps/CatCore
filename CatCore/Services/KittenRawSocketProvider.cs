using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

		// Thread signal.
		private readonly SemaphoreSlim _allDone = new SemaphoreSlim(1, 1);
		private readonly ILogger _logger;

		private bool _isServerRunning;

		internal CancellationTokenSource? ServerCts {  get; private set; }

#pragma warning disable 649
		public Action<ClientSocket>? OnConnect;
		public Action<ClientSocket, ReceivedData>? OnReceive;
		public Action<ClientSocket>? OnDisconnect;
#pragma warning restore 649

		public KittenRawSocketProvider(ILogger logger)
		{
			_logger = logger;
		}

		private void ValidateServerNotRunning()
		{
			if (_allDone.CurrentCount > 0 || _isServerRunning || !ServerCts?.IsCancellationRequested != null)
			{
				throw new InvalidOperationException("The server is still running, what is wrong with you? The poor kitty can't handle two socket servers! ;-;");
			}
		}

		private async void StartListening(CancellationTokenSource cts)
		{
			ServerCts = cts;

			// Establish the local endpoint for the socket.
			// The DNS name of the computer
			// running the listener is "host.contoso.com".
			IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(Dns.GetHostName());
			IPAddress ipAddress = ipHostInfo.AddressList[0];
			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, SOCKET_PORT);

			// Create a TCP/IP socket.
			Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			// Bind the socket to the local endpoint and listen for incoming connections.
			try
			{
				listener.Bind(localEndPoint);
				listener.Listen(20); //back log is amount of clients allowed to wait

				_isServerRunning = true;
				while (!ServerCts.IsCancellationRequested)
				{
					// Set the event to nonsignaled state.
					_allDone.Release();

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
				_logger.Fatal(e.Message, e);
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

			ClientSocket clientSocket = new ClientSocket(handler, guid, ServerCts!, HandleDisconnect);
			_connectedClients[guid] = clientSocket;



			OnConnect?.Invoke(clientSocket);

			// Create the state object.
			ReceivedData state = new ReceivedData(clientSocket);
			handler.BeginReceive(state.Buffer, 0, ReceivedData.BUFFER_SIZE, 0, ReadCallback, state);
		}

		private void ReadCallback(IAsyncResult ar)
		{

			// Retrieve the state object and the handler socket
			// from the asynchronous state object.
			ReceivedData state = (ReceivedData) ar.AsyncState;
			ClientSocket handler = state.ClientSocket;

			if (!handler.WorkSocket.Connected)
			{
				HandleDisconnect(handler);
				return;
			}

			// Read data from the client socket.
			var bytesRead = handler.WorkSocket.EndReceive(ar);

			// If 0, no more data is coming
			if (bytesRead <= 0)
			{
				return;
			}

			// There  might be more data, so store the data received so far.
			state.ReceivedDataStr.Append(Encoding.UTF8.GetString(state.Buffer, 0, bytesRead));

			// Check for end-of-file tag. If it is not there, read
			// more data.
			string content = state.ReceivedDataStr.ToString();
			if (content.IndexOf("\n", StringComparison.Ordinal) > -1)
			{
				OnReceive?.Invoke(handler, state);
			}
			else
			{
				// Not all data received. Get more.
				// Not all data gets received at once, so keep checking for more
				handler.WorkSocket.BeginReceive(state.Buffer, 0, ReceivedData.BUFFER_SIZE, 0, ReadCallback, state);
			}
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
		}

		public void Initialize()
		{
			ValidateServerNotRunning();

			ServerCts = new CancellationTokenSource();
			Task.Run(() =>
			{
				StartListening(ServerCts);
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