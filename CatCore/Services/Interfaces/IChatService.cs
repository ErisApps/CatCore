using System;
using CatCore.Models.Shared;

namespace CatCore.Services.Interfaces
{
	public interface IChatService
	{
		/// <summary>
		/// Callback that occurs when a successful login to the provided streaming service occurs
		/// </summary>
		public event Action<IPlatformService>? OnLogin;

		/// <summary>
		/// Callback that occurs when a text message is received
		/// </summary>
		public event Action<IPlatformService, IChatMessage>? OnTextMessageReceived;

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

		public void SendMessage(IChatChannel channel, string message);
	}
}