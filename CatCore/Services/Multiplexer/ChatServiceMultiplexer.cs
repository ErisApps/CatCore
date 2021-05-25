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
	public class ChatServiceMultiplexer : IChatService
	{
		private readonly ILogger _logger;
		private readonly ITwitchService _twitchPlatformService;

		public event Action<IPlatformService>? OnLogin;
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
				platformService.OnLogin += ChatServiceOnOnLogin;
				platformService.OnJoinChannel += ChatServiceOnOnJoinChannel;
				platformService.OnLeaveChannel += ChatServiceOnOnLeaveChannel;
				platformService.OnRoomStateUpdated += ChatServiceOnOnRoomStateUpdated;
				platformService.OnTextMessageReceived += ChatServiceOnOnTextMessageReceived;
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

		private void ChatServiceOnOnLogin(IPlatformService scv)
		{
			OnLogin?.Invoke(scv);
		}

		private void ChatServiceOnOnJoinChannel(IPlatformService scv, IChatChannel channel)
		{
			OnJoinChannel?.Invoke(scv, channel);
		}

		private void ChatServiceOnOnLeaveChannel(IPlatformService scv, IChatChannel channel)
		{
			OnLeaveChannel?.Invoke(scv, channel);
		}

		private void ChatServiceOnOnRoomStateUpdated(IPlatformService scv, IChatChannel channel)
		{
			OnRoomStateUpdated?.Invoke(scv, channel);
		}

		private void ChatServiceOnOnTextMessageReceived(IPlatformService scv, IChatMessage message)
		{
			OnTextMessageReceived?.Invoke(scv, message);
		}
	}
}