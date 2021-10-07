namespace CatCore.Models.Twitch.PubSub
{
	internal static class PubSubTopics
	{
		public const string VIDEO_PLAYBACK = "video-playback-by-id";

		public static string FormatVideoPlaybackTopic(string channelId) => VIDEO_PLAYBACK + "." + channelId;

		// TODO: Check feasibility implementing all topics below
		// == Globally available ================
		// video-playback-by-id.{_channelId}
		// stream-change-by-channel.{_channelId}
		// stream-chat-room-v1.{_channelId}
		// raid.{_channelId}
		// following.{_channelId}
		// chat_moderator_actions.{_twitchAuthService.LoggedInUser!.Value.UserId}.{_channelId}
		// community-points-channel-v1.{_channelId}
		// chatrooms-user-v1.{channelId}

		// == Only on token channel =============
		// channel-bits-events-v1.{_channelId}
		// channel-bits-events-v2.{_channelId}
		// channel-bits-badge-unlocks.{_channelId}
		// channel-points-channel-v1.{_channelId}
		// channel-subscribe-events-v1.{_channelId}

		// whispers.{_channelId}

		// == Unsure ============================
		// leaderboard-events-v1.bits-usage-by-channel-v1-{_channelId}-ALLTIME
		// leaderboard-events-v1.sub-gifts-sent-{_channelId}-ALLTIME
		// channel-cheer-events-public-v1.{_channelId}

		// hype-train-events-v1.{_channelId}
		// polls.{_channelId}
		// predictions-channel-v1.{_channelId}
	}
}