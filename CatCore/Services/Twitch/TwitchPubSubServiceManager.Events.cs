using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CatCore.Helpers;
using CatCore.Models.Twitch.PubSub;
using CatCore.Models.Twitch.PubSub.Responses;
using CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1;
using CatCore.Models.Twitch.PubSub.Responses.Polls;
using CatCore.Models.Twitch.PubSub.Responses.Predictions;
using CatCore.Models.Twitch.PubSub.Responses.VideoPlayback;

namespace CatCore.Services.Twitch
{
	internal sealed partial class TwitchPubSubServiceManager
	{
		#region General

		private readonly SemaphoreSlim _topicRegistrationLocker = new(1, 1);
		private readonly HashSet<string> _topicsWithRegisteredCallbacks = new();

		private void RegisterTopicWhenNeeded(string topic)
		{
			using var _ = Synchronization.Lock(_topicRegistrationLocker);
			if (!_topicsWithRegisteredCallbacks.Add(topic))
			{
				_logger.Warning("Topic was already requested by previous callbacks");
				return;
			}

			SendListenRequestToAgentsInternal(topic);
		}

		private void SendListenRequestToAgentsInternal(string topic)
		{
			var selfUserId = _twitchAuthService.FetchLoggedInUserInfo()?.UserId;
			if (CanRegisterTopicOnAllChannels(topic))
			{
				foreach (var twitchPubSubServiceExperimentalAgent in _activePubSubConnections)
				{
					twitchPubSubServiceExperimentalAgent.Value.RequestTopicListening(topic);
				}
			}
			else if (selfUserId != null && _activePubSubConnections.TryGetValue(selfUserId, out var selfPubSubServiceExperimentalAgent))
			{
				selfPubSubServiceExperimentalAgent.RequestTopicListening(topic);
			}
		}

		private void SendAllCurrentTopicsToAgentInternal(string channelId, TwitchPubSubServiceExperimentalAgent agent)
		{
			var isSelfAgent =  _twitchAuthService.FetchLoggedInUserInfo()?.UserId == channelId;
			using var _ = Synchronization.Lock(_topicRegistrationLocker);
			foreach (var topic in _topicsWithRegisteredCallbacks)
			{
				if (CanRegisterTopicOnAllChannels(topic) || isSelfAgent)
				{
					agent.RequestTopicListening(topic);
				}
			}
		}

		private void UnregisterTopicWhenNeeded(string topic)
		{
			void RequestTopicDeregistration()
			{
				using var _ = Synchronization.Lock(_topicRegistrationLocker);
				if (!_topicsWithRegisteredCallbacks.Remove(topic))
				{
					return;
				}

				SendUnlistenRequestToAgentsInternal(topic);
			}

			switch (topic)
			{
				case PubSubTopics.VIDEO_PLAYBACK:
					if (_viewCountCallbackRegistrations.IsEmpty && _streamUpCallbackRegistrations.IsEmpty && _streamDownCallbackRegistrations.IsEmpty && _commercialCallbackRegistrations.IsEmpty)
					{
						RequestTopicDeregistration();
					}

					break;
				default:
					RequestTopicDeregistration();
					break;
			}
		}

		private void SendUnlistenRequestToAgentsInternal(string topic)
		{
			var selfUserId = _twitchAuthService.FetchLoggedInUserInfo()?.UserId;
			if (CanRegisterTopicOnAllChannels(topic))
			{
				foreach (var twitchPubSubServiceExperimentalAgent in _activePubSubConnections)
				{
					twitchPubSubServiceExperimentalAgent.Value.RequestTopicUnlistening(topic);
				}
			}
			else
			{
				if (selfUserId != null && _activePubSubConnections.TryGetValue(selfUserId, out var selfPubSubServiceExperimentalAgent))
				{
					selfPubSubServiceExperimentalAgent.RequestTopicUnlistening(topic);
				}
			}
		}

		private static bool CanRegisterTopicOnAllChannels(string topic)
		{
			return topic switch
			{
				PubSubTopics.CHANNEL_POINTS_CHANNEL_V1 => false,
				_ => true
			};
		}

		#endregion

		#region video-playback-by-id

		private readonly ConcurrentDictionary<Action<string, ViewCountUpdate>, bool> _viewCountCallbackRegistrations = new();
		private readonly ConcurrentDictionary<Action<string, StreamUp>, bool> _streamUpCallbackRegistrations = new();
		private readonly ConcurrentDictionary<Action<string, StreamDown>, bool> _streamDownCallbackRegistrations = new();
		private readonly ConcurrentDictionary<Action<string, Commercial>, bool> _commercialCallbackRegistrations = new();

		private void NotifyOnViewCountUpdated(string channelId, ViewCountUpdate viewCountUpdateData)
		{
			foreach (var action in _viewCountCallbackRegistrations.Keys)
			{
				action(channelId, viewCountUpdateData);
			}
		}

		private void NotifyOnStreamUp(string channelId, StreamUp streamUpData)
		{
			foreach (var action in _streamUpCallbackRegistrations.Keys)
			{
				action(channelId, streamUpData);
			}
		}

		private void NotifyOnStreamDown(string channelId, StreamDown streamDownData)
		{
			foreach (var action in _streamDownCallbackRegistrations.Keys)
			{
				action(channelId, streamDownData);
			}
		}

		private void NotifyOnCommercial(string channelId, Commercial commercialData)
		{
			foreach (var action in _commercialCallbackRegistrations.Keys)
			{
				action(channelId, commercialData);
			}
		}

		/// <inheritdoc />
		public event Action<string, ViewCountUpdate> OnViewCountUpdated
		{
			add
			{
				if (_viewCountCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(PubSubTopics.VIDEO_PLAYBACK);
				}
				else
				{
					_logger.Warning("Callback was already registered for EventHandler {Name}", nameof(OnViewCountUpdated));
				}
			}
			remove
			{
				if (_viewCountCallbackRegistrations.TryRemove(value, out _) && _viewCountCallbackRegistrations.IsEmpty)
				{
					UnregisterTopicWhenNeeded(PubSubTopics.VIDEO_PLAYBACK);
				}
			}
		}

		/// <inheritdoc />
		public event Action<string, StreamUp> OnStreamUp
		{
			add
			{
				if (_streamUpCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(PubSubTopics.VIDEO_PLAYBACK);
				}
				else
				{
					_logger.Warning("Callback was already registered for EventHandler {Name}", nameof(OnStreamUp));
				}
			}
			remove
			{
				if (_streamUpCallbackRegistrations.TryRemove(value, out _) && _streamUpCallbackRegistrations.IsEmpty)
				{
					UnregisterTopicWhenNeeded(PubSubTopics.VIDEO_PLAYBACK);
				}
			}
		}

		/// <inheritdoc />
		public event Action<string, StreamDown> OnStreamDown
		{
			add
			{
				if (_streamDownCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(PubSubTopics.VIDEO_PLAYBACK);
				}
				else
				{
					_logger.Warning("Callback was already registered for EventHandler {Name}", nameof(OnStreamDown));
				}
			}
			remove
			{
				if (_streamDownCallbackRegistrations.TryRemove(value, out _) && _streamDownCallbackRegistrations.IsEmpty)
				{
					UnregisterTopicWhenNeeded(PubSubTopics.VIDEO_PLAYBACK);
				}
			}
		}

		/// <inheritdoc />
		public event Action<string, Commercial> OnCommercial
		{
			add
			{
				if (_commercialCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(PubSubTopics.VIDEO_PLAYBACK);
				}
				else
				{
					_logger.Warning("Callback was already registered for EventHandler {Name}", nameof(OnCommercial));
				}
			}
			remove
			{
				if (_commercialCallbackRegistrations.TryRemove(value, out _) && _commercialCallbackRegistrations.IsEmpty)
				{
					UnregisterTopicWhenNeeded(PubSubTopics.VIDEO_PLAYBACK);
				}
			}
		}

		#endregion

		#region following

		private readonly ConcurrentDictionary<Action<string, Follow>, bool> _followingCallbackRegistrations = new();

		private void NotifyOnFollow(string channelId, Follow followData)
		{
			foreach (var action in _followingCallbackRegistrations.Keys)
			{
				action(channelId, followData);
			}
		}

		/// <inheritdoc />
		public event Action<string, Follow> OnFollow
		{
			add
			{
				if (_followingCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(PubSubTopics.FOLLOWING);
				}
				else
				{
					_logger.Warning("Callback was already registered for EventHandler {Name}", nameof(OnFollow));
				}
			}
			remove
			{
				if (_followingCallbackRegistrations.TryRemove(value, out _) && _followingCallbackRegistrations.IsEmpty)
				{
					UnregisterTopicWhenNeeded(PubSubTopics.FOLLOWING);
				}
			}
		}

		#endregion

		#region polls

		private readonly ConcurrentDictionary<Action<string, PollData>, bool> _pollCallbackRegistrations = new();

		private void NotifyOnPoll(string channelId, PollData followData)
		{
			foreach (var action in _pollCallbackRegistrations.Keys)
			{
				action(channelId, followData);
			}
		}

		/// <inheritdoc />
		public event Action<string, PollData> OnPoll
		{
			add
			{
				if (_pollCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(PubSubTopics.POLLS);
				}
				else
				{
					_logger.Warning("Callback was already registered for EventHandler {Name}", nameof(OnPoll));
				}
			}
			remove
			{
				if (_pollCallbackRegistrations.TryRemove(value, out _) && _pollCallbackRegistrations.IsEmpty)
				{
					UnregisterTopicWhenNeeded(PubSubTopics.POLLS);
				}
			}
		}

		#endregion

		#region prediction

		private readonly ConcurrentDictionary<Action<string, PredictionData>, bool> _predictionsCallbackRegistrations = new();

		private void NotifyOnPrediction(string channelId, PredictionData rewardRedeemedData)
		{
			foreach (var action in _predictionsCallbackRegistrations.Keys)
			{
				action(channelId, rewardRedeemedData);
			}
		}

		/// <inheritdoc />
		public event Action<string, PredictionData> OnPrediction
		{
			add
			{
				if (_predictionsCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(PubSubTopics.PREDICTIONS);
				}
				else
				{
					_logger.Warning("Callback was already registered for EventHandler {Name}", nameof(OnPrediction));
				}
			}
			remove
			{
				if (_predictionsCallbackRegistrations.TryRemove(value, out _) && _predictionsCallbackRegistrations.IsEmpty)
				{
					UnregisterTopicWhenNeeded(PubSubTopics.PREDICTIONS);
				}
			}
		}

		#endregion

		#region channel-points-channel-v1

		private readonly ConcurrentDictionary<Action<string, RewardRedeemedData>, bool> _rewardRedeemedCallbackRegistrations = new();

		private void NotifyOnRewardRedeemed(string channelId, RewardRedeemedData rewardRedeemedData)
		{
			foreach (var action in _rewardRedeemedCallbackRegistrations.Keys)
			{
				action(channelId, rewardRedeemedData);
			}
		}

		/// <inheritdoc />
		public event Action<string, RewardRedeemedData> OnRewardRedeemed
		{
			add
			{
				if (_rewardRedeemedCallbackRegistrations.TryAdd(value, false))
				{
					RegisterTopicWhenNeeded(PubSubTopics.CHANNEL_POINTS_CHANNEL_V1);
				}
				else
				{
					_logger.Warning("Callback was already registered for EventHandler {Name}", nameof(OnRewardRedeemed));
				}
			}
			remove
			{
				if (_rewardRedeemedCallbackRegistrations.TryRemove(value, out _) && _rewardRedeemedCallbackRegistrations.IsEmpty)
				{
					UnregisterTopicWhenNeeded(PubSubTopics.CHANNEL_POINTS_CHANNEL_V1);
				}
			}
		}

		#endregion
	}
}