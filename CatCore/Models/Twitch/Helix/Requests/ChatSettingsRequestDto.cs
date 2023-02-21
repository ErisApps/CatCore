using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests
{
	internal readonly struct ChatSettingsRequestDto
	{
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("emote_mode")]
		public bool? EmoteMode { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("follower_mode")]
		public bool? FollowerMode { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("follower_mode_duration")]
		public uint? FollowerModeDurationMinutes { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("non_moderator_chat_delay")]
		public bool? NonModeratorChatDelay { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("non_moderator_chat_delay_duration")]
		public uint? NonModeratorChatDelayDurationSeconds { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("slow_mode")]
		public bool? SlowMode { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("slow_mode_wait_time")]
		public uint? SlowModeWaitTimeSeconds { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("subscriber_mode")]
		public bool? SubscriberMode { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("unique_chat_mode")]
		public bool? UniqueChatMode { get; }

		public ChatSettingsRequestDto(bool? emoteMode, bool? followerMode, uint? followerModeDurationMinutes, bool? nonModeratorChatDelay, uint? nonModeratorChatDelayDurationSeconds, bool? slowMode,
			uint? slowModeWaitTimeSeconds, bool? subscriberMode, bool? uniqueChatMode)
		{
			EmoteMode = emoteMode;
			FollowerMode = followerMode;
			FollowerModeDurationMinutes = followerModeDurationMinutes;
			NonModeratorChatDelay = nonModeratorChatDelay;
			NonModeratorChatDelayDurationSeconds = nonModeratorChatDelayDurationSeconds;
			SlowMode = slowMode;
			SlowModeWaitTimeSeconds = slowModeWaitTimeSeconds;
			SubscriberMode = subscriberMode;
			UniqueChatMode = uniqueChatMode;
		}
	}
}