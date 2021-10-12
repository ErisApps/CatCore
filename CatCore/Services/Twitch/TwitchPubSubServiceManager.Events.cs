using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CatCore.Helpers;
using CatCore.Models.Twitch.PubSub;
using CatCore.Models.Twitch.PubSub.Responses;

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
			var selfUserId = _twitchAuthService.LoggedInUser?.UserId;
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
			var isSelfAgent =  _twitchAuthService.LoggedInUser?.UserId == channelId;
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
				default:
					RequestTopicDeregistration();
					break;
			}
		}

		private void SendUnlistenRequestToAgentsInternal(string topic)
		{
			var selfUserId = _twitchAuthService.LoggedInUser?.UserId;
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
				_ => true
			};
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
					_logger.Warning("Callback was already registered");
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
	}
}