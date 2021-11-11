using System;
using System.Collections.Generic;
using System.Linq;
using CatCore.Models.Shared;
using CatCore.Models.Twitch;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Multiplexer
{
	public sealed class ChatServiceMultiplexer : IChatService
	{
		private readonly ILogger _logger;
		private readonly ITwitchService _twitchPlatformService;

		public event Action<IPlatformService>? OnAuthenticatedStateChanged;
		public event Action<IPlatformService>? OnChatConnected;
		public event Action<IPlatformService, IChatMessage>? OnTextMessageReceived;
		public event Action<IPlatformService, IChatChannel>? OnJoinChannel;
		public event Action<IPlatformService, IChatChannel>? OnLeaveChannel;
		public event Action<IPlatformService, IChatChannel>? OnRoomStateUpdated;

		public ChatServiceMultiplexer(ILogger logger, IList<IPlatformService> platformServices)
		{
			_logger = logger;

			foreach (var platformService in platformServices)
			{
				// TODO: Register to all event handlers of IChatService
				platformService.OnAuthenticatedStateChanged += ChatServiceOnAuthenticatedStateChanged;
				platformService.OnChatConnected += ChatServiceOnChatConnected;
				platformService.OnJoinChannel += ChatServiceOnJoinChannel;
				platformService.OnLeaveChannel += ChatServiceOnLeaveChannel;
				platformService.OnRoomStateUpdated += ChatServiceOnRoomStateUpdated;
				platformService.OnTextMessageReceived += ChatServiceOnTextMessageReceived;
			}

			_twitchPlatformService = platformServices.OfType<ITwitchService>().First();
		}

		public ITwitchService GetTwitchPlatformService()
		{
			return _twitchPlatformService;
		}

		public void SendMessage(IChatChannel channel, string message)
		{
			switch (channel)
			{
				case TwitchChannel _:
					GetTwitchPlatformService().SendMessage(channel, message);
					break;
				default:
					_logger.Error("Sending a message of type {Type} isn't supported (yet)", channel.GetType().Name);
					throw new NotSupportedException();
			}
		}

		private void ChatServiceOnAuthenticatedStateChanged(IPlatformService scv)
		{
			OnAuthenticatedStateChanged?.Invoke(scv);
		}

		private void ChatServiceOnChatConnected(IPlatformService scv)
		{
			OnChatConnected?.Invoke(scv);
		}

		private void ChatServiceOnJoinChannel(IPlatformService scv, IChatChannel channel)
		{
			OnJoinChannel?.Invoke(scv, channel);
		}

		private void ChatServiceOnLeaveChannel(IPlatformService scv, IChatChannel channel)
		{
			OnLeaveChannel?.Invoke(scv, channel);
		}

		private void ChatServiceOnRoomStateUpdated(IPlatformService scv, IChatChannel channel)
		{
			OnRoomStateUpdated?.Invoke(scv, channel);
		}

		private void ChatServiceOnTextMessageReceived(IPlatformService scv, IChatMessage message)
		{
			OnTextMessageReceived?.Invoke(scv, message);
		}
	}
}