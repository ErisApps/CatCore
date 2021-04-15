using System;
using System.Threading.Tasks;
using Websocket.Client;
using Websocket.Client.Models;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenWebSocketProvider
	{
		bool IsConnected { get; }
		Task Connect(string uri, TimeSpan? heartBeatInterval = null, string? customHeartBeatMessage = null);
		Task Disconnect(string? reason = null);
		void SendMessage(string message);

		event Action<ReconnectionInfo>? ReconnectHappened;
		event Action<DisconnectionInfo>? DisconnectHappened;
		event Action<ResponseMessage>? MessageReceived;
	}
}