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
		private readonly ITwitchRoomStateTrackerService _twitchRoomStateTrackerService;
		private readonly ITwitchUserStateTrackerService _twitchUserStateTrackerService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;

		internal TwitchService(ILogger logger, ITwitchIrcService twitchIrcService, ITwitchPubSubServiceManager twitchPubSubServiceManager, ITwitchHelixApiService twitchHelixApiService,
			ITwitchRoomStateTrackerService twitchRoomStateTrackerService, ITwitchUserStateTrackerService twitchUserStateTrackerService, ITwitchChannelManagementService twitchChannelManagementService)
		{
			_logger = logger;
			_twitchIrcService = twitchIrcService;
			_twitchPubSubServiceManager = twitchPubSubServiceManager;
			_twitchHelixApiService = twitchHelixApiService;
			_twitchRoomStateTrackerService = twitchRoomStateTrackerService;
			_twitchUserStateTrackerService = twitchUserStateTrackerService;
			_twitchChannelManagementService = twitchChannelManagementService;
		}

		public ITwitchPubSubServiceManager GetPubSubService() => _twitchPubSubServiceManager;
		public ITwitchHelixApiService GetHelixApiService() => _twitchHelixApiService;
		public ITwitchRoomStateTrackerService GetRoomStateTrackerService() => _twitchRoomStateTrackerService;
		public ITwitchUserStateTrackerService GetUserStateTrackerService() => _twitchUserStateTrackerService;
		public ITwitchChannelManagementService GetChannelManagementService() => _twitchChannelManagementService;

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

		public IChatChannel? DefaultChannel => _twitchChannelManagementService.GetOwnChannel();

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