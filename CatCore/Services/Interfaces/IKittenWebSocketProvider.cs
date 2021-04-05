using System;
using System.Threading.Tasks;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenWebSocketProvider
	{
		bool IsConnected { get; }

		event Action? OnOpen;
		event Action? OnClose;
		event Action<string>? OnMessageReceived;

		Task Connect(string uri, TimeSpan? heartBeatInterval = null, string? customHeartBeatMessage = null);
		Task Disconnect(string? reason = null);
		void SendMessage(string message);
	}
}