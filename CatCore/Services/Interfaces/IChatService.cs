using System;
using CatCore.Models.Shared;

namespace CatCore.Services.Interfaces
{
	public interface IChatService
	{
		/// <summary>
		/// Callback that occurs when the authentication state changes for the provided streaming service
		/// </summary>
		public event Action<IPlatformService>? OnAuthenticatedStateChanged;

		/// <summary>
		/// Callback that occurs when the provided streaming service successfully connected to the chat
		/// </summary>
		public event Action<IPlatformService>? OnChatConnected;

		/// <summary>
		/// Callback that occurs when the user joins a chat channel
		/// </summary>
		public event Action<IPlatformService, IChatChannel>? OnJoinChannel;

		/// <summary>
		/// Callback that occurs when a chat channel receives updated info
		/// </summary>
		public event Action<IPlatformService, IChatChannel>? OnRoomStateUpdated;

		/// <summary>
		/// Callback that occurs when the user leaves a chat channel
		/// </summary>
		public event Action<IPlatformService, IChatChannel>? OnLeaveChannel;

		/// <summary>
		/// Callback that occurs when a text message is received
		/// </summary>
		public event Action<IPlatformService, IChatMessage>? OnTextMessageReceived;

		public void SendMessage(IChatChannel channel, string message);
	}
}