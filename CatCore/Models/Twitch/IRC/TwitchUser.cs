using System.Collections.ObjectModel;
using CatCore.Models.Shared;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.IRC
{
	public sealed class TwitchUser : IChatUser
	{
		[PublicAPI]
		public string Id { get; internal set; }

		[PublicAPI]
		public string UserName { get; internal set; }

		[PublicAPI]
		public string DisplayName { get; internal set; }

		[PublicAPI]
		public string Color { get; internal set; }

		[PublicAPI]
		public bool IsModerator { get; internal set; }

		[PublicAPI]
		public bool IsBroadcaster { get; internal set; }

		[PublicAPI]
		public bool IsSubscriber { get; internal set; }

		[PublicAPI]
		public bool IsTurbo { get; internal set; }

		[PublicAPI]
		public bool IsVip { get; internal set; }

		[PublicAPI]
		public ReadOnlyCollection<IChatBadge> Badges { get; }

		public TwitchUser(string id, string userName, string displayName, string color, bool isModerator, bool isBroadcaster, bool isSubscriber, bool isTurbo, bool isVip,
			ReadOnlyCollection<IChatBadge> badges)
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
			Badges = badges;
		}
	}
}