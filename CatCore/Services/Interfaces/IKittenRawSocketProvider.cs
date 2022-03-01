using System;
using CatCore.Services.Sockets;
using CatCore.Services.Sockets.Packets;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenRawSocketProvider : INeedInitialization, IDisposable
	{
		bool isServerRunning();

#pragma warning disable 649
		event Action<ClientSocket>? OnConnect;
		event Action<ClientSocket, Packet?, string>? OnReceive;
		event Action<ClientSocket>? OnDisconnect;
#pragma warning restore 649


	}
}