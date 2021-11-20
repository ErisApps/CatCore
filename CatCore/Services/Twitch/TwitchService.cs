using System;
using System.Threading.Tasks;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
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

		async Task IPlatformService<ITwitchService, TwitchChannel, TwitchMessage>.Start()
		{
			_logger.Information("Initializing {Type}", nameof(TwitchService));

			RegisterInternalEventHandlers();

			await _twitchIrcService.Start();
			await _twitchPubSubServiceManager.Start();
		}

		async Task IPlatformService<ITwitchService, TwitchChannel, TwitchMessage>.Stop()
		{
			_logger.Information("Stopped {Type}", nameof(TwitchService));

			DeregisterInternalEventHandlers();

			await _twitchIrcService.Stop();
			await _twitchPubSubServiceManager.Stop();
		}

		public bool LoggedIn => _twitchAuthService.HasTokens && _twitchAuthService.TokenIsValid;
		public TwitchChannel? DefaultChannel => _twitchChannelManagementService.GetOwnChannel();

		public event Action<ITwitchService>? OnAuthenticatedStateChanged;
		public event Action<ITwitchService>? OnChatConnected;
		public event Action<ITwitchService, TwitchChannel>? OnJoinChannel;
		public event Action<ITwitchService, TwitchChannel>? OnLeaveChannel;
		public event Action<ITwitchService, TwitchChannel>? OnRoomStateUpdated;
		public event Action<ITwitchService, TwitchMessage>? OnTextMessageReceived;

		public void SendMessage(TwitchChannel channel, string message)
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

		private void TwitchIrcServiceOnJoinChannel(TwitchChannel channel)
		{
			OnJoinChannel?.Invoke(this, channel);
		}

		private void TwitchIrcServiceOnLeaveChannel(TwitchChannel channel)
		{
			OnLeaveChannel?.Invoke(this, channel);
		}

		private void TwitchIrcServiceOnRoomStateChanged(TwitchChannel channel)
		{
			OnRoomStateUpdated?.Invoke(this, channel);
		}

		private void TwitchIrcServiceOnMessageReceived(TwitchMessage message)
		{
			OnTextMessageReceived?.Invoke(this, message);
		}
	}
}