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
	internal class TwitchPubSubServiceManager : ITwitchPubSubServiceManager
	{
		private readonly ILogger _logger;
		private readonly ThreadSafeRandomFactory _randomFactory;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;

		private readonly Dictionary<string, TwitchPubSubServiceAgent> _activePubSubConnections;

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

			_activePubSubConnections = new Dictionary<string, TwitchPubSubServiceAgent>();
		}

		async Task ITwitchPubSubServiceManager.Start()
		{
			foreach (var channelId in _twitchChannelManagementService.GetAllActiveChannelIds())
			{
				await CreatePubSubAgent(channelId).ConfigureAwait(false);
			}
		}

		async Task ITwitchPubSubServiceManager.Stop()
		{
			foreach (var twitchPubSubServiceAgent in _activePubSubConnections)
			{
				await twitchPubSubServiceAgent.Value.Stop().ConfigureAwait(false);
			}

			_activePubSubConnections.Clear();
		}

		private async void TwitchAuthServiceOnOnCredentialsChanged()
		{
			if (_twitchAuthService.HasTokens)
			{
				if (_activeStateManager.GetState(PlatformType.Twitch))
				{
					foreach (var twitchPubSubServiceAgent in _activePubSubConnections)
					{
						await twitchPubSubServiceAgent.Value.Start().ConfigureAwait(false);
					}
				}
			}
			else
			{
				foreach (var twitchPubSubServiceAgent in _activePubSubConnections)
				{
					await twitchPubSubServiceAgent.Value.Stop().ConfigureAwait(false);
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
						await twitchPubSubServiceAgent.Stop().ConfigureAwait(false);
						_activePubSubConnections.Remove(disabledChannel.Key);
					}
				}

				foreach (var enabledChannel in args.EnabledChannels)
				{
					await CreatePubSubAgent(enabledChannel.Key).ConfigureAwait(false);
				}
			}
		}

		private async Task CreatePubSubAgent(string channelId)
		{
			var agent = new TwitchPubSubServiceAgent(_logger, _randomFactory.CreateNewRandom(), _twitchAuthService, channelId);
			await agent.Start().ConfigureAwait(false);

			_activePubSubConnections[channelId] = agent;
		}
	}
}