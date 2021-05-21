using System;
using System.Threading.Tasks;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenWebSocketProvider
	{
		bool IsConnected { get; }
		Task Connect(string uri);
		Task Disconnect(string? reason = null);
		void SendMessage(string message);
		Task SendMessageInstant(string message);

		event Action? ConnectHappened;
		event Action? DisconnectHappened;
		event Action<string>? MessageReceived;
	}
}