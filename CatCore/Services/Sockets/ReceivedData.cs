using System;
using System.Net.Sockets;
using System.Text;

namespace CatCore.Services.Sockets
{
	// State object for reading client data asynchronously
	public class ReceivedData
	{
		// Size of receive buffer.
		public const int BUFFER_SIZE = 4096;

		// Receive buffer.
		// the buffer is used to store the received bytes temporarily
		// and cleared when they are later parsed into receivedData
		public readonly byte[] Buffer = new byte[BUFFER_SIZE];

		// Received data string.
		public readonly StringBuilder ReceivedDataStr = new StringBuilder();

		// Client socket.
		public readonly ClientSocket ClientSocket;

		public readonly Guid ClientUuid;

		public readonly Socket WorkSocket;

		public ReceivedData(ClientSocket clientSocket)
		{
			ClientSocket = clientSocket;
			ClientUuid = clientSocket.Uuid;
			WorkSocket = clientSocket.WorkSocket;
		}
	}
}