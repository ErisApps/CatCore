namespace CatCore.Models.Twitch.IRC
{
	public class TwitchRoomState
	{
		public string BroadcasterLang { get; }

		/// <summary>
		/// The id of the channel to which the room state belongs.
		/// </summary>
		public string RoomId { get; }

		/// <summary>
		/// If enabled, only emotes are allowed in chat.
		/// </summary>
		public bool EmoteOnly { get; }

		/// <summary>
		/// If enabled, controls which followers can chat.
		/// </summary>
		public bool FollowersOnly { get; }

		/// <summary>
		/// If enabled, only subscribers and moderators can chat.
		/// </summary>
		public bool SubscribersOnly { get; }

		/// <summary>
		/// If enabled, messages with more than 9 characters must be unique.
		/// </summary>
		public bool R9K { get; }

		/// <summary>
		/// The number of seconds a chatter without moderator privileges must wait between sending messages.
		/// </summary>
		public int SlowModeInterval { get; }

		/// <summary>
		/// If FollowersOnly is true, this specifies the number of minutes a user must be following before they can chat.
		/// </summary>
		public int MinFollowTime { get; }

		public TwitchRoomState(string broadcasterLang, string roomId, bool emoteOnly, bool followersOnly, bool subscribersOnly, bool r9K, int slowModeInterval, int minFollowTime)
		{
			BroadcasterLang = broadcasterLang;
			RoomId = roomId;
			EmoteOnly = emoteOnly;
			FollowersOnly = followersOnly;
			SubscribersOnly = subscribersOnly;
			R9K = r9K;
			SlowModeInterval = slowModeInterval;
			MinFollowTime = minFollowTime;
		}
	}
}