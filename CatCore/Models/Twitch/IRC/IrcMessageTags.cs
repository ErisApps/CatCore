namespace CatCore.Models.Twitch.IRC
{
	/// <summary>
	/// All IRC message tag keys that are publicly described by Twitch documentation
	/// </summary>
	/// <remarks>See also: https://dev.twitch.tv/docs/irc/tags</remarks>
	public static class IrcMessageTags
	{
		public const string BAN_DURATION = "ban-duration";
		public const string LOGIN = "login";
		public const string TARGET_MSG_ID = "target-msg-id";
		public const string BADGE_INFO = "badge-info";
		public const string BADGES = "badges";
		public const string COLOR = "color";
		public const string DISPLAY_NAME = "display-name";
		public const string EMOTE_SETS = "emote-sets";
		public const string TURBO = "turbo";
		public const string USER_ID = "user-id";
		public const string USER_TYPE = "user-type";
		public const string BITS = "bits";
		public const string EMOTES = "emotes";
		public const string ID = "id";
		public const string MOD = "mod";
		public const string ROOM_ID = "room-id";
		public const string SUBSCRIBER = "subscriber";
		public const string TMI_SENT_TS = "tmi-sent-ts";
		public const string EMOTE_ONLY = "emote-only";
		public const string FOLLOWERS_ONLY = "followers-only";
		public const string R9_K = "r9k";
		public const string SLOW = "slow";
		public const string SUBS_ONLY = "subs-only";
		public const string SYSTEM_MSG = "system-msg";
		public const string MSG_ID = "msg-id";

		public const string REPLY_PARENT_MSG_ID = "reply-parent-msg-id";
		public const string REPLY_PARENT_USER_ID = "reply-parent-user-id";
		public const string REPLY_PARENT_USER_LOGIN = "reply-parent-user-login";
		public const string REPLY_PARENT_DISPLAY_NAME = "reply-parent-display-name";
		public const string REPLY_PARENT_MSG_BODY = "reply-parent-msg-body";

		public const string MSG_PARAM_CUMULATIVE_MONTHS = "msg-param-cumulative-months";
		public const string MSG_PARAM_DISPLAY_NAME = "msg-param-displayName";
		public const string MSG_PARAM_LOGIN = "msg-param-login";
		public const string MSG_PARAM_MONTHS = "msg-param-months";
		public const string MSG_PARAM_PROMO_GIFT_TOTAL = "msg-param-promo-gift-total";
		public const string MSG_PARAM_PROMO_NAME = "msg-param-promo-name";
		public const string MSG_PARAM_RECIPIENT_DISPLAY_NAME = "msg-param-recipient-display-name";
		public const string MSG_PARAM_RECIPIENT_ID = "msg-param-recipient-id";
		public const string MSG_PARAM_RECIPIENT_USER_NAME = "msg-param-recipient-user-name";
		public const string MSG_PARAM_SENDER_LOGIN = "msg-param-sender-login";
		public const string MSG_PARAM_SENDER_NAME = "msg-param-sender-name";
		public const string MSG_PARAM_SHOULD_SHARE_STREAK = "msg-param-should-share-streak";
		public const string MSG_PARAM_STREAK_MONTHS = "msg-param-streak-months";
		public const string MSG_PARAM_SUB_PLAN = "msg-param-sub-plan";
		public const string MSG_PARAM_SUB_PLAN_NAME = "msg-param-sub-plan-name";
		public const string MSG_PARAM_VIEWER_COUNT = "msg-param-viewerCount";
		public const string MSG_PARAM_RITUAL_NAME = "msg-param-ritual-name";
		public const string MSG_PARAM_THRESHOLD = "msg-param-threshold";
		public const string MSG_PARAM_GIFT_MONTHS = "msg-param-gift-months";
	}
}