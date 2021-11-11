using System;
using CatCore.Models.Shared;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	public sealed class TwitchService : ITwitchService
	{
		private readonly ILogger _logger;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchIrcService _twitchIrcService;
		private readonly ITwitchPubSubServiceManager _twitchPubSubServiceManager;
		private readonly ITwitchHelixApiService _twitchHelixApiService;
		private readonly ITwitchRoomStateTrackerService _twitchRoomStateTrackerService;
		private readonly ITwitchUserStateTrackerService _twitchUserStateTrackerService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;

		internal TwitchService(ILogger logger, ITwitchAuthService twitchAuthService, ITwitchIrcService twitchIrcService, ITwitchPubSubServiceManager twitchPubSubServiceManager,
			ITwitchHelixApiService twitchHelixApiService, ITwitchRoomStateTrackerService twitchRoomStateTrackerService, ITwitchUserStateTrackerService twitchUserStateTrackerService,
			ITwitchChannelManagementService twitchChannelManagementService)
		{
			_logger = logger;
			_twitchAuthService = twitchAuthService;
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

		public bool LoggedIn => _twitchAuthService.HasTokens && _twitchAuthService.TokenIsValid;
		public IChatChannel? DefaultChannel => _twitchChannelManagementService.GetOwnChannel();

		public event Action<IPlatformService>? OnAuthenticatedStateChanged;
		public event Action<IPlatformService>? OnChatConnected;
		public event Action<IPlatformService, IChatChannel>? OnJoinChannel;
		public event Action<IPlatformService, IChatChannel>? OnLeaveChannel;
		public event Action<IPlatformService, IChatChannel>? OnRoomStateUpdated;
		public event Action<IPlatformService, IChatMessage>? OnTextMessageReceived;

		public void SendMessage(IChatChannel channel, string message)
		{
			_twitchIrcService.SendMessage(channel, message);
		}

		private void RegisterInternalEventHandlers()
		{
			DeregisterInternalEventHandlers();

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnCredentialsChanged;

			_twitchIrcService.OnChatConnected += TwitchIrcServiceOnChatConnected;
			_twitchIrcService.OnJoinChannel += TwitchIrcServiceOnJoinChannel;
			_twitchIrcService.OnLeaveChannel += TwitchIrcServiceOnLeaveChannel;
			_twitchIrcService.OnRoomStateChanged += TwitchIrcServiceOnRoomStateChanged;
			_twitchIrcService.OnMessageReceived += TwitchIrcServiceOnMessageReceived;
		}

		private void DeregisterInternalEventHandlers()
		{
			_twitchAuthService.OnCredentialsChanged -= TwitchAuthServiceOnCredentialsChanged;

			_twitchIrcService.OnChatConnected -= TwitchIrcServiceOnChatConnected;
			_twitchIrcService.OnJoinChannel -= TwitchIrcServiceOnJoinChannel;
			_twitchIrcService.OnLeaveChannel -= TwitchIrcServiceOnLeaveChannel;
			_twitchIrcService.OnRoomStateChanged -= TwitchIrcServiceOnRoomStateChanged;
			_twitchIrcService.OnMessageReceived -= TwitchIrcServiceOnMessageReceived;
		}

		private void TwitchAuthServiceOnCredentialsChanged()
		{
			OnAuthenticatedStateChanged?.Invoke(this);
		}

		private void TwitchIrcServiceOnChatConnected()
		{
			OnChatConnected?.Invoke(this);
		}

		private void TwitchIrcServiceOnJoinChannel(IChatChannel channel)
		{
			OnJoinChannel?.Invoke(this, channel);
		}

		private void TwitchIrcServiceOnLeaveChannel(IChatChannel channel)
		{
			OnLeaveChannel?.Invoke(this, channel);
		}

		private void TwitchIrcServiceOnRoomStateChanged(IChatChannel channel)
		{
			OnRoomStateUpdated?.Invoke(this, channel);
		}

		private void TwitchIrcServiceOnMessageReceived(IChatMessage message)
		{
			OnTextMessageReceived?.Invoke(this, message);
		}
	}
}