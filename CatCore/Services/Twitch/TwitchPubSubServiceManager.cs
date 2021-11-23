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

			using var _ = await Synchronization.LockAsync(_topicRegistrationLocker);
			foreach (var topic in _topicsWithRegisteredCallbacks)
			{
				SendListenRequestToAgentsInternal(topic);
			}
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

				SendAllCurrentTopicsToAgentInternal(channelId, CreatePubSubAgent(channelId));
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
					SendAllCurrentTopicsToAgentInternal(enabledChannel.Key, CreatePubSubAgent(enabledChannel.Key));
				}
			}
		}

		private TwitchPubSubServiceExperimentalAgent CreatePubSubAgent(string channelId)
		{
			var agent = new TwitchPubSubServiceExperimentalAgent(_logger, _randomFactory.CreateNewRandom(), _twitchAuthService, _activeStateManager, channelId, _topicsWithRegisteredCallbacks);

			agent.OnViewCountUpdate += NotifyOnViewCountUpdated;
			agent.OnStreamUp += NotifyOnStreamUp;
			agent.OnStreamDown += NotifyOnStreamDown;
			agent.OnCommercial += NotifyOnCommercial;

			agent.OnFollow += NotifyOnFollow;
			agent.OnPoll += NotifyOnPoll;
			agent.OnPrediction += NotifyOnPrediction;
			agent.OnRewardRedeemed += NotifyOnRewardRedeemed;

			return _activePubSubConnections[channelId] = agent;
		}

		private async Task DestroyPubSubAgent(string channelId, TwitchPubSubServiceExperimentalAgent twitchPubSubServiceAgent)
		{
			twitchPubSubServiceAgent.OnViewCountUpdate -= NotifyOnViewCountUpdated;
			twitchPubSubServiceAgent.OnStreamUp -= NotifyOnStreamUp;
			twitchPubSubServiceAgent.OnStreamDown -= NotifyOnStreamDown;
			twitchPubSubServiceAgent.OnCommercial -= NotifyOnCommercial;

			twitchPubSubServiceAgent.OnFollow -= NotifyOnFollow;
			twitchPubSubServiceAgent.OnPoll -= NotifyOnPoll;
			twitchPubSubServiceAgent.OnPrediction -= NotifyOnPrediction;
			twitchPubSubServiceAgent.OnRewardRedeemed -= NotifyOnRewardRedeemed;

			await twitchPubSubServiceAgent.DisposeAsync().ConfigureAwait(false);

			_activePubSubConnections.Remove(channelId);
		}
	}
}