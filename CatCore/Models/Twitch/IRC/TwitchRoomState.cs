using JetBrains.Annotations;

namespace CatCore.Models.Twitch.IRC
{
	public sealed class TwitchRoomState
	{
		/// <summary>
		/// The id of the channel to which the room state belongs.
		/// </summary>
		[PublicAPI]
		public string RoomId { get; internal set; }

		/// <summary>
		/// If enabled, only emotes are allowed in chat.
		/// </summary>
		[PublicAPI]
		public bool EmoteOnly { get; internal set; }

		/// <summary>
		/// If enabled, controls which followers can chat.
		/// </summary>
		[PublicAPI]
		public bool FollowersOnly { get; internal set; }

		/// <summary>
		/// If enabled, only subscribers and moderators can chat.
		/// </summary>
		[PublicAPI]
		public bool SubscribersOnly { get; internal set; }

		/// <summary>
		/// If enabled, messages with more than 9 characters must be unique.
		/// </summary>
		[PublicAPI]
		public bool R9K { get; internal set; }

		/// <summary>
		/// The number of seconds a chatter without moderator privileges must wait between sending messages.
		/// </summary>
		[PublicAPI]
		public int SlowModeInterval { get; internal set; }

		/// <summary>
		/// If FollowersOnly is true, this specifies the number of minutes a user must be following before they can chat.
		/// </summary>
		[PublicAPI]
		public int MinFollowTime { get; internal set; }

		public TwitchRoomState(string roomId, bool emoteOnly, bool followersOnly, bool subscribersOnly, bool r9K, int slowModeInterval, int minFollowTime)
		{
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