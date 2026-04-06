using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub
{
	internal struct EventSubChatMessagePayload
	{
		[JsonPropertyName("subscription")]
		public EventSubSubscription Subscription { get; init; }

		[JsonPropertyName("event")]
		public EventSubChatMessageEvent Event { get; init; }
	}

	[PublicAPI]
	public struct EventSubSubscription
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("version")]
		public string Version { get; init; }

		[JsonPropertyName("status")]
		public string Status { get; init; }

		[JsonPropertyName("condition")]
		public Dictionary<string, string> Condition { get; init; }

		[JsonPropertyName("transport")]
		public Dictionary<string, string> Transport { get; init; }

		[JsonPropertyName("created_at")]
		public string CreatedAt { get; init; }

		[JsonPropertyName("cost")]
		public int Cost { get; init; }
	}

	[PublicAPI]
	public struct EventSubChatMessageEvent
	{
		[JsonPropertyName("broadcaster_user_id")]
		public string BroadcasterUserId { get; init; }

		[JsonPropertyName("broadcaster_user_login")]
		public string BroadcasterUserLogin { get; init; }

		[JsonPropertyName("broadcaster_user_name")]
		public string BroadcasterUserName { get; init; }

		[JsonPropertyName("chatter_user_id")]
		public string ChatterUserId { get; init; }

		[JsonPropertyName("chatter_user_login")]
		public string ChatterUserLogin { get; init; }

		[JsonPropertyName("chatter_user_name")]
		public string ChatterUserName { get; init; }

		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		[JsonPropertyName("message")]
		public EventSubChatMessageContent Message { get; init; }

		[JsonPropertyName("color")]
		public string Color { get; init; }

		[JsonPropertyName("badges")]
		public List<EventSubBadge> Badges { get; init; }

		[JsonPropertyName("message_type")]
		public string MessageType { get; init; }

		[JsonPropertyName("cheer")]
		public EventSubCheer? Cheer { get; init; }

		[JsonPropertyName("reply")]
		public EventSubReply? Reply { get; init; }

		[JsonPropertyName("channel_points_custom_reward_id")]
		public string? ChannelPointsCustomRewardId { get; init; }
	}

	[PublicAPI]
	public struct EventSubChatMessageContent
	{
		[JsonPropertyName("text")]
		public string Text { get; init; }

		[JsonPropertyName("fragments")]
		public List<EventSubFragment> Fragments { get; init; }
	}

	[PublicAPI]
	public struct EventSubFragment
	{
		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("text")]
		public string Text { get; init; }

		[JsonPropertyName("emote")]
		public EventSubEmoteFragment? Emote { get; init; }

		[JsonPropertyName("cheermote")]
		public EventSubCheermoteFragment? Cheermote { get; init; }

		[JsonPropertyName("mention")]
		public EventSubMentionFragment? Mention { get; init; }
	}

	[PublicAPI]
	public struct EventSubEmoteFragment
	{
		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("emote_set_id")]
		public string EmoteSetId { get; init; }

		[JsonPropertyName("owner_id")]
		public string OwnerId { get; init; }

		[JsonPropertyName("format")]
		public List<string> Format { get; init; }
	}

	[PublicAPI]
	public struct EventSubCheermoteFragment
	{
		[JsonPropertyName("bits")]
		public int Bits { get; init; }

		[JsonPropertyName("tier")]
		public int Tier { get; init; }
	}

	[PublicAPI]
	public struct EventSubMentionFragment
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }
	}

	[PublicAPI]
	public struct EventSubBadge
	{
		[JsonPropertyName("set_id")]
		public string SetId { get; init; }

		[JsonPropertyName("id")]
		public string Id { get; init; }

		[JsonPropertyName("info")]
		public string Info { get; init; }
	}

	[PublicAPI]
	public struct EventSubCheer
	{
		[JsonPropertyName("bits")]
		public int Bits { get; init; }
	}

	[PublicAPI]
	public struct EventSubReply
	{
		[JsonPropertyName("parent_message_id")]
		public string ParentMessageId { get; init; }

		[JsonPropertyName("parent_message_body")]
		public string ParentMessageBody { get; init; }

		[JsonPropertyName("parent_user_id")]
		public string ParentUserId { get; init; }

		[JsonPropertyName("parent_user_login")]
		public string ParentUserLogin { get; init; }

		[JsonPropertyName("parent_user_name")]
		public string ParentUserName { get; init; }
	}
}
