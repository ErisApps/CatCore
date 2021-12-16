using System;
using System.Collections.Generic;
using System.Linq;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;

namespace CatCore.Services.Multiplexer
{
	public sealed class ChatServiceMultiplexer : IChatService<MultiplexedPlatformService, MultiplexedChannel, MultiplexedMessage>
	{
		private readonly ITwitchService _twitchPlatformService;

		public event Action<MultiplexedPlatformService>? OnAuthenticatedStateChanged;
		public event Action<MultiplexedPlatformService>? OnChatConnected;
		public event Action<MultiplexedPlatformService, MultiplexedMessage>? OnTextMessageReceived;
		public event Action<MultiplexedPlatformService, MultiplexedChannel>? OnJoinChannel;
		public event Action<MultiplexedPlatformService, MultiplexedChannel>? OnLeaveChannel;
		public event Action<MultiplexedPlatformService, MultiplexedChannel>? OnRoomStateUpdated;
		public event Action<MultiplexedPlatformService, MultiplexedChannel, string>? OnMessageDeleted;
		public event Action<MultiplexedPlatformService, MultiplexedChannel, string?>? OnChatCleared;

		public ChatServiceMultiplexer(IList<MultiplexedPlatformService> platformServices)
		{
			foreach (var platformService in platformServices)
			{
				// TODO: Register to all event handlers of IChatService
				platformService.OnAuthenticatedStateChanged += ChatServiceOnAuthenticatedStateChanged;
				platformService.OnChatConnected += ChatServiceOnChatConnected;
				platformService.OnJoinChannel += ChatServiceOnJoinChannel;
				platformService.OnLeaveChannel += ChatServiceOnLeaveChannel;
				platformService.OnRoomStateUpdated += ChatServiceOnRoomStateUpdated;
				platformService.OnTextMessageReceived += ChatServiceOnTextMessageReceived;
				platformService.OnMessageDeleted += ChatServiceOnMessageDeleted;
				platformService.OnChatCleared += ChatServiceOnChatCleared;
			}

			_twitchPlatformService = platformServices.Select(s => s.Underlying).OfType<ITwitchService>().First();
		}

		public ITwitchService GetTwitchPlatformService() => _twitchPlatformService;

		private void ChatServiceOnAuthenticatedStateChanged(MultiplexedPlatformService scv)
		{
			OnAuthenticatedStateChanged?.Invoke(scv);
		}

		private void ChatServiceOnChatConnected(MultiplexedPlatformService scv)
		{
			OnChatConnected?.Invoke(scv);
		}

		private void ChatServiceOnJoinChannel(MultiplexedPlatformService scv, MultiplexedChannel channel)
		{
			OnJoinChannel?.Invoke(scv, channel);
		}

		private void ChatServiceOnLeaveChannel(MultiplexedPlatformService scv, MultiplexedChannel channel)
		{
			OnLeaveChannel?.Invoke(scv, channel);
		}

		private void ChatServiceOnRoomStateUpdated(MultiplexedPlatformService scv, MultiplexedChannel channel)
		{
			OnRoomStateUpdated?.Invoke(scv, channel);
		}

		private void ChatServiceOnTextMessageReceived(MultiplexedPlatformService scv, MultiplexedMessage message)
		{
			OnTextMessageReceived?.Invoke(scv, message);
		}

		private void ChatServiceOnMessageDeleted(MultiplexedPlatformService scv, MultiplexedChannel channel, string deletedMessageId)
		{
			OnMessageDeleted?.Invoke(scv, channel, deletedMessageId);
		}

		private void ChatServiceOnChatCleared(MultiplexedPlatformService scv, MultiplexedChannel channel, string? username)
		{
			OnChatCleared?.Invoke(scv, channel, username);
		}
	}
}