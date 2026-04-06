using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub
{
	internal struct EventSubChatClearPayload
	{
		[JsonPropertyName("subscription")]
		public EventSubSubscription Subscription { get; init; }

		[JsonPropertyName("event")]
		public EventSubChatClearEvent Event { get; init; }
	}

	public struct EventSubChatClearEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("moderator_user_id")]
		public string ModeratorUserId { get; init; }

		[JsonPropertyName("moderator_user_login")]
		public string ModeratorUserLogin { get; init; }

		[JsonPropertyName("moderator_user_name")]
		public string ModeratorUserName { get; init; }
	}
}
