using System;
using System.Threading.Tasks;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchIrcService
	{
		event Action? OnChatConnected;
		event Action<TwitchChannel>? OnJoinChannel;
		event Action<TwitchChannel>? OnLeaveChannel;
		event Action<TwitchChannel>? OnRoomStateChanged;
		event Action<TwitchMessage>? OnMessageReceived;
		event Action<TwitchChannel, string>? OnMessageDeleted;
		event Action<TwitchChannel, string?>? OnChatCleared;

		void SendMessage(TwitchChannel channel, string message);

		public Task Start();
		public Task Stop();
	}
}