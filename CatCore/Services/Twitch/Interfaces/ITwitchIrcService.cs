using System;
using System.Threading.Tasks;
using CatCore.Models.Shared;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchIrcService
	{
		event Action? OnLogin;
		event Action<IChatChannel>? OnJoinChannel;
		event Action<IChatChannel>? OnLeaveChannel;
		event Action<IChatChannel>? OnRoomStateChanged;
		event Action<IChatMessage>? OnMessageReceived;

		public Task Start();
		public Task Stop();
	}
}