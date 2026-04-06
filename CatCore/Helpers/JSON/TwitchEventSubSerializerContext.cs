using System.Text.Json.Serialization;
using CatCore.Models.Twitch.EventSub;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(EventSubWebSocketMessage<EventSubSessionPayload>))]
	[JsonSerializable(typeof(EventSubSessionPayload))]
	[JsonSerializable(typeof(EventSubSessionInfo))]
	[JsonSerializable(typeof(EventSubWebSocketMessage<EventSubChatMessagePayload>))]
	[JsonSerializable(typeof(EventSubChatMessagePayload))]
	[JsonSerializable(typeof(EventSubSubscription))]
	[JsonSerializable(typeof(EventSubChatMessageEvent))]
	[JsonSerializable(typeof(EventSubChatMessageContent))]
	[JsonSerializable(typeof(EventSubFragment))]
	[JsonSerializable(typeof(EventSubEmoteFragment))]
	[JsonSerializable(typeof(EventSubCheermoteFragment))]
	[JsonSerializable(typeof(EventSubMentionFragment))]
	[JsonSerializable(typeof(EventSubBadge))]
	[JsonSerializable(typeof(EventSubCheer))]
	[JsonSerializable(typeof(EventSubReply))]
	[JsonSerializable(typeof(EventSubWebSocketMessage<EventSubChatNotificationPayload>))]
	[JsonSerializable(typeof(EventSubChatNotificationPayload))]
	[JsonSerializable(typeof(EventSubChatNotificationEvent))]
	[JsonSerializable(typeof(EventSubNotificationSub))]
	[JsonSerializable(typeof(EventSubNotificationResub))]
	[JsonSerializable(typeof(EventSubNotificationGiftSub))]
	[JsonSerializable(typeof(EventSubNotificationRaid))]
	[JsonSerializable(typeof(EventSubNotificationUnraid))]
	[JsonSerializable(typeof(EventSubNotificationPayItForward))]
	[JsonSerializable(typeof(EventSubNotificationAnnouncement))]
	[JsonSerializable(typeof(EventSubNotificationBitsBadgeTier))]
	[JsonSerializable(typeof(EventSubWebSocketMessage<EventSubChatMessageDeletePayload>))]
	[JsonSerializable(typeof(EventSubChatMessageDeletePayload))]
	[JsonSerializable(typeof(EventSubChatMessageDeleteEvent))]
	[JsonSerializable(typeof(EventSubWebSocketMessage<EventSubChatClearPayload>))]
	[JsonSerializable(typeof(EventSubChatClearPayload))]
	[JsonSerializable(typeof(EventSubChatClearEvent))]
	[JsonSerializable(typeof(EventSubWebSocketMessage<EventSubChatSettingsPayload>))]
	[JsonSerializable(typeof(EventSubChatSettingsPayload))]
	[JsonSerializable(typeof(EventSubChatSettingsEvent))]
	internal partial class TwitchEventSubSerializerContext : JsonSerializerContext
	{
	}
}
