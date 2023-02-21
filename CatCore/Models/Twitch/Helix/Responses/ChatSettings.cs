using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct ChatSettings
	{
		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; }

		[JsonPropertyName("emote_mode")]
		public bool EmoteMode { get; }

		[JsonPropertyName("follower_mode")]
		public bool FollowerMode { get; }

		[JsonPropertyName("follower_mode_duration")]
		public uint? FollowerModeDurationMinutes { get; }

		[JsonPropertyName("moderator_id")]
		public string? ModeratorId { get; }

		[JsonPropertyName("non_moderator_chat_delay")]
		public bool NonModeratorChatDelay { get; }

		[JsonPropertyName("non_moderator_chat_delay_duration")]
		public uint? NonModeratorChatDelayDurationSeconds { get; }

		[JsonPropertyName("slow_mode")]
		public bool SlowMode { get; }

		[JsonPropertyName("slow_mode_wait_time")]
		public uint? SlowModeWaitTimeSeconds { get; }

		[JsonPropertyName("subscriber_mode")]
		public bool SubscriberMode { get; }

		[JsonPropertyName("unique_chat_mode")]
		public bool UniqueChatMode { get; }

		[JsonConstructor]
		public ChatSettings(string broadcasterId, bool emoteMode, bool followerMode, uint? followerModeDurationMinutes, string moderatorId, bool nonModeratorChatDelay,
			uint? nonModeratorChatDelayDurationSeconds, bool slowMode, uint? slowModeWaitTimeSeconds, bool subscriberMode, bool uniqueChatMode)
		{
			BroadcasterId = broadcasterId;
			EmoteMode = emoteMode;
			SlowMode = slowMode;
			SlowModeWaitTimeSeconds = slowModeWaitTimeSeconds;
			ModeratorId = moderatorId;
			NonModeratorChatDelay = nonModeratorChatDelay;
			NonModeratorChatDelayDurationSeconds = nonModeratorChatDelayDurationSeconds;
			FollowerMode = followerMode;
			FollowerModeDurationMinutes = followerModeDurationMinutes;
			SubscriberMode = subscriberMode;
			UniqueChatMode = uniqueChatMode;
		}
	}
}