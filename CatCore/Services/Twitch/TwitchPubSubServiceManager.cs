using System.Collections.Generic;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.EventArgs;
using CatCore.Models.Shared;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal sealed partial class TwitchPubSubServiceManager : ITwitchPubSubServiceManager
	{
		private readonly ILogger _logger;
		private readonly ThreadSafeRandomFactory _randomFactory;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;

		private readonly Dictionary<string, TwitchPubSubServiceExperimentalAgent> _activePubSubConnections;

		public TwitchPubSubServiceManager(ILogger logger, ThreadSafeRandomFactory randomFactory, IKittenPlatformActiveStateManager activeStateManager, ITwitchAuthService twitchAuthService,
			ITwitchChannelManagementService twitchChannelManagementService)
		{
			_logger = logger;
			_randomFactory = randomFactory;
			_activeStateManager = activeStateManager;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;

			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnOnCredentialsChanged;
			_twitchChannelManagementService.ChannelsUpdated += TwitchChannelManagementServiceOnChannelsUpdated;

			_activePubSubConnections = new Dictionary<string, TwitchPubSubServiceExperimentalAgent>();
		}

		async Task ITwitchPubSubServiceManager.Start()
		{
			foreach (var channelId in _twitchChannelManagementService.GetAllActiveChannelIds())
			{
				CreatePubSubAgent(channelId);
			}

			await Task.CompletedTask;
		}

		async Task ITwitchPubSubServiceManager.Stop()
		{
			foreach (var twitchPubSubServiceExperimentalAgent in _activePubSubConnections)
			{
				await DestroyPubSubAgent(twitchPubSubServiceExperimentalAgent.Key, twitchPubSubServiceExperimentalAgent.Value).ConfigureAwait(false);
			}
		}

		private void TwitchAuthServiceOnOnCredentialsChanged()
		{
			if (!_twitchAuthService.HasTokens || !_activeStateManager.GetState(PlatformType.Twitch))
			{
				return;
			}

			foreach (var channelId in _twitchChannelManagementService.GetAllActiveChannelIds())
			{
				if (_activePubSubConnections.ContainsKey(channelId))
				{
					continue;
				}
			}
		}

		private async void TwitchChannelManagementServiceOnChannelsUpdated(object sender, TwitchChannelsUpdatedEventArgs args)
		{
			if (_activeStateManager.GetState(PlatformType.Twitch))
			{
				foreach (var disabledChannel in args.DisabledChannels)
				{
					if (_activePubSubConnections.TryGetValue(disabledChannel.Key, out var twitchPubSubServiceAgent))
					{
						await DestroyPubSubAgent(disabledChannel.Key, twitchPubSubServiceAgent).ConfigureAwait(false);
					}
				}

				foreach (var enabledChannel in args.EnabledChannels)
				{
					CreatePubSubAgent(enabledChannel.Key);
				}
			}
		}

		private TwitchPubSubServiceExperimentalAgent CreatePubSubAgent(string channelId)
		{
			var agent = new TwitchPubSubServiceExperimentalAgent(_logger, _randomFactory.CreateNewRandom(), _twitchAuthService, _activeStateManager, channelId);

			return _activePubSubConnections[channelId] = agent;
		}

		private async Task DestroyPubSubAgent(string channelId, TwitchPubSubServiceExperimentalAgent twitchPubSubServiceAgent)
		{
			await twitchPubSubServiceAgent.DisposeAsync().ConfigureAwait(false);

			_activePubSubConnections.Remove(channelId);
		}
	}
}