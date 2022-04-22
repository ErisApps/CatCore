using System.Threading.Tasks;
using CatCore.Helpers;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenWebSocketProvider
	{
		bool IsConnected { get; }
		Task Connect(string uri);
		Task Disconnect(string? reason = null);

		event AsyncEventHandlerDefinitions.AsyncEventHandler<WebSocketConnection>? ConnectHappened;
		event AsyncEventHandlerDefinitions.AsyncEventHandler? DisconnectHappened;
		event AsyncEventHandlerDefinitions.AsyncEventHandler<WebSocketConnection, string>? MessageReceived;
	}
}