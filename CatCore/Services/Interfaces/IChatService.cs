using System;
using CatCore.Models.Shared;

namespace CatCore.Services.Interfaces
{
	public interface IChatService<out TChat, out TChannel, out TMessage>
		where TChat : IChatService<TChat, TChannel, TMessage>
		where TChannel : IChatChannel<TChannel, TMessage>
		where TMessage : IChatMessage<TMessage, TChannel>
	{
		/// <summary>
		/// Callback that occurs when the authentication state changes for the provided streaming service
		/// </summary>
		public event Action<TChat>? OnAuthenticatedStateChanged;

		/// <summary>
		/// Callback that occurs when the provided streaming service successfully connected to the chat
		/// </summary>
		public event Action<TChat>? OnChatConnected;

		/// <summary>
		/// Callback that occurs when the user joins a chat channel
		/// </summary>
		public event Action<TChat, TChannel>? OnJoinChannel;

		/// <summary>
		/// Callback that occurs when a chat channel receives updated info
		/// </summary>
		public event Action<TChat, TChannel>? OnRoomStateUpdated;

		/// <summary>
		/// Callback that occurs when the user leaves a chat channel
		/// </summary>
		public event Action<TChat, TChannel>? OnLeaveChannel;

		/// <summary>
		/// Callback that occurs when a text message is received
		/// </summary>
		public event Action<TChat, TMessage>? OnTextMessageReceived;

		/// <summary>
		/// Callback that occurs when a chat message gets deleted
		/// </summary>
		public event Action<TChat, TChannel, string> OnMessageDeleted;

		/// <summary>
		/// Callback that occurs when the chat of a particular channel or all messages of a user in a channel gets cleared
		/// </summary>
		public event Action<TChat, TChannel, string?>? OnChatCleared;
	}
}