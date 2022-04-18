using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WebsocketClientLite.PCL;

namespace CatCore.Services
{
	public class WebSocketConnection
	{
		private readonly MessageWebsocketRx _wss;

		public WebSocketConnection(MessageWebsocketRx websocketClient)
		{
			_wss = websocketClient;
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
				await _wss.GetSender().SendText(message).ConfigureAwait(false);
			}
		}
	}
}