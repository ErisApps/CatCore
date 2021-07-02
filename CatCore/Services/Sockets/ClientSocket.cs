using System;
using System.Collections.Concurrent;
using System.IO;
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

		private readonly BlockingCollection<Packet> _packetsToSend = new BlockingCollection<Packet>();
		private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(0,1);
		private readonly SemaphoreSlim _receiveSemaphore = new SemaphoreSlim(0,1);

		private readonly Action<ClientSocket> _onClose;
		private readonly Action<ClientSocket, ReceivedData> _onRead;

		public ClientSocket(Socket workSocket, Guid uuid, CancellationTokenSource cts, Action<ClientSocket> onClose, Action<ClientSocket, ReceivedData> onReceive)
		{
			Uuid = uuid;
			WorkSocket = workSocket;
			_onClose = onClose;
			_onRead = onReceive;

			Task.Run(async () => {
				await SendTaskLoop(cts);
			}, cts.Token);

			Task.Run(async () =>
			{
				await ReceiveTaskLoop(cts);
			}, cts.Token);

		}

		private async Task SendTaskLoop(CancellationTokenSource cts)
		{
			while (!cts.IsCancellationRequested)
			{
				// Stop trying to send data
				if (!WorkSocket.Connected)
				{
					return;
				}

				if (!_packetsToSend.TryTake(out var packet))
				{
					await Task.Yield();
					continue;
				}

				var bytesToSend = JsonSerializer.SerializeToUtf8Bytes(packet, packet.GetType());
				SendData(bytesToSend);

				await _sendSemaphore.WaitAsync(cts.Token);
			}
		}

		private async Task ReceiveTaskLoop(CancellationTokenSource cts)
		{
			while (!cts.IsCancellationRequested)
			{
				// Stop trying to receive data
				if (!WorkSocket.Connected)
				{
					return;
				}

				// Create the state object.
				ReceivedData state = new ReceivedData(this);
				WorkSocket.BeginReceive(state.Buffer, 0, ReceivedData.BUFFER_SIZE, 0, ReadCallback, state);

				await _receiveSemaphore.WaitAsync(cts.Token);
			}
		}


		private void ReadCallback(IAsyncResult ar)
		{
			// Retrieve the state object and the handler socket
			// from the asynchronous state object.
			ReceivedData state = (ReceivedData) ar.AsyncState;
			ClientSocket handler = state.ClientSocket;

			if (!handler.WorkSocket.Connected)
			{
				Close();
				return;
			}

			// Read data from the client socket.
			var bytesRead = handler.WorkSocket.EndReceive(ar);

			// If 0, no more data is coming
			if (bytesRead <= 0)
			{
				_receiveSemaphore.Release();
				_onRead(this, state);
				return;
			}

			// There  might be more data, so store the data received so far.
			state.ReceivedDataStr.Append(Encoding.UTF8.GetString(state.Buffer, 0, bytesRead));

			_receiveSemaphore.Release();
			_onRead(this, state);
		}

		private void SendCallback(IAsyncResult ar)
		{
			try
			{
				// Retrieve the socket from the state object.
				Socket handler = (Socket) ar.AsyncState;

				// Complete sending the data to the remote device.
				handler.EndSend(ar);

				_sendSemaphore.Release();
			}
			catch (Exception e)
			{
				// todo: use logger?
				Console.WriteLine(e.ToString());
			}
		}

		private void SendData(byte[] data)
		{
			// synchronous sending
			// var bytesSent = WorkSocket.Send(data.ToArray());
			//
			// if (bytesSent < data.Length)
			// {
			// 	SendData(data.Slice(bytesSent, data.Length).ToArray());
			// }

			// async sending
			// Begin sending the data to the remote device.
			WorkSocket.BeginSend(data, 0, data.Length, 0, SendCallback, WorkSocket);
		}

		public async Task QueueSend(Packet packet)
		{
			if (!WorkSocket.Connected)
			{
				throw new IOException("Socket has been closed!");
			}

			// Avoid blocking, is this overkill?
			await Task.Run(() =>
			{
				_packetsToSend.Add(packet);
			});
		}

		public void Close()
		{
			try
			{
				_sendSemaphore.Release();
				_sendSemaphore.Dispose();
			}
			catch (Exception e)
			{
				// todo: use logger?
				Console.WriteLine(e.ToString());
			}

			_onClose.Invoke(this);
		}
	}

}