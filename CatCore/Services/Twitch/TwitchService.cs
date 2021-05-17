using System;
using CatCore.Models.Shared;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	public class TwitchService : ITwitchService
	{
		private readonly ILogger _logger;
		private readonly ITwitchIrcService _twitchIrcService;
		private readonly ITwitchPubSubServiceManager _twitchPubSubServiceManager;
		private readonly ITwitchHelixApiService _twitchHelixApiService;

		internal TwitchService(ILogger logger, ITwitchIrcService twitchIrcService, ITwitchPubSubServiceManager twitchPubSubServiceManager, ITwitchHelixApiService twitchHelixApiService)
		{
			_logger = logger;
			_twitchIrcService = twitchIrcService;
			_twitchPubSubServiceManager = twitchPubSubServiceManager;
			_twitchHelixApiService = twitchHelixApiService;
		}

		public ITwitchPubSubServiceManager GetPubSubService() => _twitchPubSubServiceManager;
		public ITwitchHelixApiService GetHelixApiService() => _twitchHelixApiService;

		void IPlatformService.Start()
		{
			_logger.Information("Initializing {Type}", nameof(TwitchService));

			RegisterInternalEventHandlers();

			_twitchIrcService.Start();
			_twitchPubSubServiceManager.Start();
		}

		void IPlatformService.Stop()
		{
			_logger.Information("Stopped {Type}", nameof(TwitchService));

			DeregisterInternalEventHandlers();

			_twitchIrcService.Stop();
			_twitchPubSubServiceManager.Stop();
		}

		public IChatChannel? DefaultChannel { get; }

		public event Action<IPlatformService>? OnLogin;
		public event Action<IPlatformService, IChatMessage>? OnTextMessageReceived;
		public event Action<IPlatformService, IChatChannel>? OnJoinChannel;
		public event Action<IPlatformService, IChatChannel>? OnLeaveChannel;
		public event Action<IPlatformService, IChatChannel>? OnRoomStateUpdated;

		public void SendMessage(IChatChannel channel, string message)
		{
			_twitchIrcService.SendMessage(channel, message);
		}

		private void RegisterInternalEventHandlers()
		{
			DeregisterInternalEventHandlers();

			_twitchIrcService.OnLogin += TwitchIrcServiceOnOnLogin;
			_twitchIrcService.OnJoinChannel += TwitchIrcServiceOnOnJoinChannel;
			_twitchIrcService.OnLeaveChannel += TwitchIrcServiceOnOnLeaveChannel;
			_twitchIrcService.OnRoomStateChanged += TwitchIrcServiceOnOnRoomStateChanged;
			_twitchIrcService.OnMessageReceived += TwitchIrcServiceOnOnMessageReceived;
		}

		private void DeregisterInternalEventHandlers()
		{
			_twitchIrcService.OnLogin -= TwitchIrcServiceOnOnLogin;
			_twitchIrcService.OnJoinChannel -= TwitchIrcServiceOnOnJoinChannel;
			_twitchIrcService.OnLeaveChannel -= TwitchIrcServiceOnOnLeaveChannel;
			_twitchIrcService.OnRoomStateChanged -= TwitchIrcServiceOnOnRoomStateChanged;
			_twitchIrcService.OnMessageReceived -= TwitchIrcServiceOnOnMessageReceived;
		}

		private void TwitchIrcServiceOnOnLogin()
		{
			OnLogin?.Invoke(this);
		}

		private void TwitchIrcServiceOnOnJoinChannel(IChatChannel channel)
		{
			OnJoinChannel?.Invoke(this, channel);
		}

		private void TwitchIrcServiceOnOnLeaveChannel(IChatChannel channel)
		{
			OnLeaveChannel?.Invoke(this, channel);
		}

		private void TwitchIrcServiceOnOnRoomStateChanged(IChatChannel channel)
		{
			OnRoomStateUpdated?.Invoke(this, channel);
		}

		private void TwitchIrcServiceOnOnMessageReceived(IChatMessage message)
		{
			OnTextMessageReceived?.Invoke(this, message);
		}
	}
}