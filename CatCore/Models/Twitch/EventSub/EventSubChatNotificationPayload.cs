using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.EventSub
{
	internal struct EventSubChatNotificationPayload
	{
		[JsonPropertyName("subscription")]
		public EventSubSubscription Subscription { get; init; }

		[JsonPropertyName("event")]
		public EventSubChatNotificationEvent Event { get; init; }
	}

	[PublicAPI]
	public struct EventSubChatNotificationEvent
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

		[JsonPropertyName("notice_type")]
		public string NoticeType { get; init; }

		[JsonPropertyName("message")]
		public EventSubChatMessageContent Message { get; init; }

		[JsonPropertyName("system_message")]
		public string? SystemMessage { get; init; }

		[JsonPropertyName("sub")]
		public EventSubNotificationSub? Sub { get; init; }

		[JsonPropertyName("resub")]
		public EventSubNotificationResub? Resub { get; init; }

		[JsonPropertyName("gift_sub")]
		public EventSubNotificationGiftSub? GiftSub { get; init; }

		[JsonPropertyName("raid")]
		public EventSubNotificationRaid? Raid { get; init; }

		[JsonPropertyName("unraid")]
		public EventSubNotificationUnraid? Unraid { get; init; }

		[JsonPropertyName("pay_it_forward")]
		public EventSubNotificationPayItForward? PayItForward { get; init; }

		[JsonPropertyName("announcement")]
		public EventSubNotificationAnnouncement? Announcement { get; init; }

		[JsonPropertyName("bits_badge_tier")]
		public EventSubNotificationBitsBadgeTier? BitsBadgeTier { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationSub
	{
		[JsonPropertyName("is_gift")]
		public bool IsGift { get; init; }

		[JsonPropertyName("sub_tier")]
		public string SubTier { get; init; }

		[JsonPropertyName("duration_months")]
		public int DurationMonths { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationResub
	{
		[JsonPropertyName("duration_months")]
		public int DurationMonths { get; init; }

		[JsonPropertyName("cumulative_months")]
		public int CumulativeMonths { get; init; }

		[JsonPropertyName("streak_months")]
		public int? StreakMonths { get; init; }

		[JsonPropertyName("is_streak")]
		public bool IsStreak { get; init; }

		[JsonPropertyName("sub_tier")]
		public string SubTier { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationGiftSub
	{
		[JsonPropertyName("community_gift_id")]
		public string? CommunityGiftId { get; init; }

		[JsonPropertyName("duration_months")]
		public int DurationMonths { get; init; }

		[JsonPropertyName("cumulative_total")]
		public int? CumulativeTotal { get; init; }

		[JsonPropertyName("recipient_user_id")]
		public string RecipientUserId { get; init; }

		[JsonPropertyName("recipient_user_login")]
		public string RecipientUserLogin { get; init; }

		[JsonPropertyName("recipient_user_name")]
		public string RecipientUserName { get; init; }

		[JsonPropertyName("sub_tier")]
		public string SubTier { get; init; }

		[JsonPropertyName("community_gift_ids")]
		public List<string>? CommunityGiftIds { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationRaid
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; init; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; init; }

		[JsonPropertyName("user_name")]
		public string UserName { get; init; }

		[JsonPropertyName("viewer_count")]
		public int ViewerCount { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationUnraid
	{
	}

	[PublicAPI]
	public struct EventSubNotificationPayItForward
	{
		[JsonPropertyName("is_anonymous")]
		public bool IsAnonymous { get; init; }

		[JsonPropertyName("community_gift_id")]
		public string? CommunityGiftId { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationAnnouncement
	{
		[JsonPropertyName("color")]
		public string Color { get; init; }
	}

	[PublicAPI]
	public struct EventSubNotificationBitsBadgeTier
	{
		[JsonPropertyName("tier")]
		public int Tier { get; init; }
	}
}
