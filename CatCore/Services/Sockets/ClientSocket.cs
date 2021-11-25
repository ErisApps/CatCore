using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Services.Sockets.Packets;

namespace CatCore.Services.Sockets
{


	// Not much, just represents a client
	public class ClientSocket
	{
		// Client socket.
		public readonly Socket WorkSocket;
		public readonly Guid Uuid;

		private readonly BlockingCollection<Packet> _packetsToSend = new();
		private readonly Action<ClientSocket> _onClose;
		private readonly Action<ClientSocket, string> _onRead;
		private readonly NetworkStream _socketStream;

		private bool _closed;

		// Size of receive buffer.
		private const int BUFFER_SIZE = 4096;
		private const char DELIMETER = '\n'; // Environment.NewLine;

		public ClientSocket(Socket workSocket, Guid uuid, CancellationTokenSource cts, Action<ClientSocket> onClose, Action<ClientSocket, string> onReceive)
		{
			Uuid = uuid;
			WorkSocket = workSocket;
			_socketStream = new NetworkStream(WorkSocket, false);

			// timeout in ms
			// todo: configurable
			_socketStream.ReadTimeout = 5000;
			_socketStream.WriteTimeout = 5000;

			_onClose = onClose;
			_onRead = onReceive;

			_ = Task.Run(async () =>
			{
				await SendTaskLoop(cts);
			}, cts.Token);


			_ = Task.Run(async () =>
			{
				await ReceiveTaskLoopStart(cts);
			}, cts.Token);
		}

		private async Task SendTaskLoop(CancellationTokenSource cts)
		{
			try
			{
				while (!cts.IsCancellationRequested && !_closed)
				{
					// Stop trying to send data
					if (!WorkSocket.Connected)
					{
						Close();
						return;
					}

					if (!_packetsToSend.TryTake(out var packet))
					{
						await Task.Yield();
						continue;
					}

					var bytesToSend = JsonSerializer.SerializeToUtf8Bytes(packet, packet.GetType());

					await _socketStream.WriteAsync(bytesToSend, 0, bytesToSend.Length, cts.Token);
					await _socketStream.FlushAsync();
				}
			}
			catch (SocketException e)
			{
				Console.Error.WriteLine(e);
				Close();
			}
		}

		private async Task ReceiveTaskLoopStart(CancellationTokenSource cts)
		{
			try
			{
				// Received data string.
				var receivedDataStr = new StringBuilder();

				void ReadFlush(StringBuilder data)
				{
					try
					{
						// All data has been finalized, invoke callback
						_onRead(this, data.ToString());
					}
					catch (Exception e)
					{
						Console.Error.WriteLine(e);
					}

					// Clear string
					data.Clear();
				}

				while (!cts.IsCancellationRequested && !_closed)
				{
					// Stop trying to receive data
					if (!WorkSocket.Connected)
					{
						Close();
						return;
					}


					// Receive buffer.
					// the buffer is used to store the received bytes temporarily
					// and cleared when they are later parsed into receivedData
					var buffer = new byte[BUFFER_SIZE];


					var bytesRead = await _socketStream.ReadAsync(buffer, 0, BUFFER_SIZE, cts.Token);

					// If 0, no more data is coming
					if (bytesRead <= 0)
					{
						ReadFlush(receivedDataStr);
					}
					else
					{
						var str = Encoding.UTF8.GetString(buffer, 0, bytesRead);

						// if the string already contains a delimiter,
						// split it. This way, multiple strings sent at once can be parsed
						if (str.Contains(DELIMETER))
						{
							var strings = str.Split(DELIMETER);

							var index = 0;

							foreach (var s in strings)
							{
								if (index >= strings.Length - 1)
								{
									break;
								}

								receivedDataStr.Append(s);

								ReadFlush(receivedDataStr);
								index++;
							}

							continue;
						}

						// There might be more data, so store the data received so far.
						receivedDataStr.Append(str);

						Console.WriteLine(receivedDataStr);
					}
				}
			}
			catch (SocketException e)
			{
				Console.Error.WriteLine(e);
				Close();
			}

			Console.WriteLine("Done listening");
		}

		public void QueueSend(Packet packet)
		{
			if (!WorkSocket.Connected)
			{
				Close();
				throw new IOException("Socket has been closed!");
			}

			_packetsToSend.Add(packet);
		}

		private void Close()
		{
			if (_closed)
			{
				return;
			}

			try
			{
				if (WorkSocket.Connected)
				{
					WorkSocket.Disconnect(true);
					WorkSocket.Close();
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}

			_socketStream.Dispose();

			_onClose.Invoke(this);
			_closed = true;
		}
	}

}