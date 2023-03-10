using System;
using System.Threading.Tasks;
using CatCore.Models.Credentials;
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

		/// <inheritdoc />
		public ITwitchPubSubServiceManager GetPubSubService() => _twitchPubSubServiceManager;

		/// <inheritdoc />
		public ITwitchHelixApiService GetHelixApiService() => _twitchHelixApiService;

		/// <inheritdoc />
		public ITwitchRoomStateTrackerService GetRoomStateTrackerService() => _twitchRoomStateTrackerService;

		/// <inheritdoc />
		public ITwitchUserStateTrackerService GetUserStateTrackerService() => _twitchUserStateTrackerService;

		/// <inheritdoc />
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

		/// <inheritdoc />
		public bool LoggedIn => _twitchAuthService is { HasTokens: true, TokenIsValid: true, Status: AuthenticationStatus.Authenticated };

		/// <inheritdoc />
		public TwitchChannel? DefaultChannel => _twitchChannelManagementService.GetOwnChannel();

		/// <inheritdoc />
		public event Action<ITwitchService>? OnAuthenticatedStateChanged;

		/// <inheritdoc />
		public event Action<ITwitchService>? OnChatConnected;

		/// <inheritdoc />
		public event Action<ITwitchService, TwitchChannel>? OnJoinChannel;

		/// <inheritdoc />
		public event Action<ITwitchService, TwitchChannel>? OnLeaveChannel;

		/// <inheritdoc />
		public event Action<ITwitchService, TwitchChannel>? OnRoomStateUpdated;

		/// <inheritdoc />
		public event Action<ITwitchService, TwitchMessage>? OnTextMessageReceived;

		/// <inheritdoc />
		public event Action<ITwitchService, TwitchChannel, string>? OnMessageDeleted;

		/// <inheritdoc />
		public event Action<ITwitchService, TwitchChannel, string?>? OnChatCleared;

		public void SendMessage(TwitchChannel channel, string message)
		{
			_twitchIrcService.SendMessage(channel, message);
		}

		private void RegisterInternalEventHandlers()
		{
			DeregisterInternalEventHandlers();

			_twitchAuthService.OnAuthenticationStatusChanged += TwitchAuthServiceOnAuthenticatedStatusChanged;

			_twitchIrcService.OnChatConnected += TwitchIrcServiceOnChatConnected;
			_twitchIrcService.OnJoinChannel += TwitchIrcServiceOnJoinChannel;
			_twitchIrcService.OnLeaveChannel += TwitchIrcServiceOnLeaveChannel;
			_twitchIrcService.OnRoomStateChanged += TwitchIrcServiceOnRoomStateChanged;
			_twitchIrcService.OnMessageReceived += TwitchIrcServiceOnMessageReceived;
			_twitchIrcService.OnMessageDeleted += TwitchIrcServiceOnMessageDeleted;
			_twitchIrcService.OnChatCleared += TwitchIrcServiceOnChatCleared;
		}

		private void DeregisterInternalEventHandlers()
		{
			_twitchAuthService.OnAuthenticationStatusChanged -= TwitchAuthServiceOnAuthenticatedStatusChanged;

			_twitchIrcService.OnChatConnected -= TwitchIrcServiceOnChatConnected;
			_twitchIrcService.OnJoinChannel -= TwitchIrcServiceOnJoinChannel;
			_twitchIrcService.OnLeaveChannel -= TwitchIrcServiceOnLeaveChannel;
			_twitchIrcService.OnRoomStateChanged -= TwitchIrcServiceOnRoomStateChanged;
			_twitchIrcService.OnMessageReceived -= TwitchIrcServiceOnMessageReceived;
			_twitchIrcService.OnMessageDeleted -= TwitchIrcServiceOnMessageDeleted;
			_twitchIrcService.OnChatCleared -= TwitchIrcServiceOnChatCleared;
		}

		private void TwitchAuthServiceOnAuthenticatedStatusChanged(AuthenticationStatus _)
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

		private void TwitchIrcServiceOnMessageDeleted(TwitchChannel channel, string deletedMessageId)
		{
			OnMessageDeleted?.Invoke(this, channel, deletedMessageId);
		}

		private void TwitchIrcServiceOnChatCleared(TwitchChannel channel, string? username)
		{
			OnChatCleared?.Invoke(this, channel, username);
		}
	}
}