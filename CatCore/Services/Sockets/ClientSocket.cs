using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CatCore.Services.Sockets
{


	// Not much, just represents a client
	public class ClientSocket
	{
		// Client socket.
		public readonly Socket WorkSocket;
		public readonly Guid Uuid;

		private readonly BlockingCollection<Packet> _packetsToSend = new BlockingCollection<Packet>();
		private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1,1);

		private readonly Action<ClientSocket> _onClose;

		public ClientSocket(Socket workSocket, Guid uuid, CancellationTokenSource cts, Action<ClientSocket> onClose)
		{
			Uuid = uuid;
			WorkSocket = workSocket;
			_onClose = onClose;

			Task.Run(async () => {
				await SendTaskLoop(cts);
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
				}

				var bytesToSend = Encoding.UTF8.GetBytes(packet.ToJson());
				SendData(bytesToSend);

				await _sendSemaphore.WaitAsync(cts.Token);
			}
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