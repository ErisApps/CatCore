using System;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenWebSocketProvider
	{
		bool IsConnected { get; }

		event Action? OnOpen;
		event Action? OnClose;
		event Action<string>? OnMessageReceived;
	}
}