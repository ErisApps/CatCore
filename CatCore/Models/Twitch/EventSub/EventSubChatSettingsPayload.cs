using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub
{
	internal struct EventSubChatSettingsPayload
	{
		[JsonPropertyName("subscription")]
		public EventSubSubscription Subscription { get; init; }

		[JsonPropertyName("event")]
		public EventSubChatSettingsEvent Event { get; init; }
	}

	public struct EventSubChatSettingsEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("emote_mode")]
		public bool EmoteMode { get; init; }

		[JsonPropertyName("follower_mode")]
		public bool FollowerMode { get; init; }

		[JsonPropertyName("follower_mode_duration_minutes")]
		public int FollowerModeDurationMinutes { get; init; }

		[JsonPropertyName("slow_mode")]
		public bool SlowMode { get; init; }

		[JsonPropertyName("slow_mode_wait_seconds")]
		public int SlowModeWaitSeconds { get; init; }

		[JsonPropertyName("subscriber_mode")]
		public bool SubscriberMode { get; init; }

		[JsonPropertyName("unique_chat_mode")]
		public bool UniqueChatMode { get; init; }
	}
}
