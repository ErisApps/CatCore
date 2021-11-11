using System;
using System.Threading.Tasks;
using CatCore.Models.Shared;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchIrcService
	{
		event Action? OnChatConnected;
		event Action<IChatChannel>? OnJoinChannel;
		event Action<IChatChannel>? OnLeaveChannel;
		event Action<IChatChannel>? OnRoomStateChanged;
		event Action<IChatMessage>? OnMessageReceived;

		void SendMessage(IChatChannel channel, string message);

		public Task Start();
		public Task Stop();
	}
}