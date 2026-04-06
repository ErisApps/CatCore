using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub
{
	internal struct EventSubChatMessageDeletePayload
	{
		[JsonPropertyName("subscription")]
		public EventSubSubscription Subscription { get; init; }

		[JsonPropertyName("event")]
		public EventSubChatMessageDeleteEvent Event { get; init; }
	}

	public struct EventSubChatMessageDeleteEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("target_user_id")]
		public string TargetUserId { get; init; }

		[JsonPropertyName("target_user_login")]
		public string TargetUserLogin { get; init; }

		[JsonPropertyName("target_user_name")]
		public string TargetUserName { get; init; }

		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }
	}
}
