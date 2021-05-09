using CatCore.Models.Shared;

namespace CatCore.Models.Twitch.IRC
{
	public class TwitchUser : IChatUser
	{
		public string Id { get; internal set; }
		public string UserName { get; internal set; }
		public string DisplayName { get; internal set; }
		public string Color { get; internal set; }
		public bool IsModerator { get; internal set; }
		public bool IsBroadcaster { get; internal set; }
		public bool IsSubscriber { get; internal set; }
		public bool IsTurbo { get; internal set; }
		public bool IsVip { get; internal set; }

		public TwitchUser(string id, string userName, string displayName, string color, bool isModerator, bool isBroadcaster, bool isSubscriber, bool isTurbo, bool isVip)
		{
			Id = id;
			UserName = userName;
			DisplayName = displayName;
			Color = color;
			IsModerator = isModerator;
			IsBroadcaster = isBroadcaster;
			IsSubscriber = isSubscriber;
			IsTurbo = isTurbo;
			IsVip = isVip;
		}
	}
}