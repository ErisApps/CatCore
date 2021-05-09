namespace CatCore.Models.Twitch.IRC
{
	public class TwitchRoomState
    {
	    public string BroadcasterLang { get; }
	    public string RoomId { get; }
        public bool EmoteOnly { get; }
        public bool FollowersOnly { get; }
        public bool SubscribersOnly { get; }
        public bool R9K { get; }

        /// <summary>
        /// The number of seconds a chatter without moderator privileges must wait between sending messages
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