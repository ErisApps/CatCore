using System;
using CatCore.Services.Sockets;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenRawSocketProvider : INeedInitialization, IDisposable
	{
		bool isServerRunning();

#pragma warning disable 649
		event Action<ClientSocket>? OnConnect;
		event Action<ClientSocket, ReceivedData>? OnReceive;
		event Action<ClientSocket>? OnDisconnect;
#pragma warning restore 649


	}
}