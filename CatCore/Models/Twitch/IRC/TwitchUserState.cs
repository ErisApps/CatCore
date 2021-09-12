using JetBrains.Annotations;

namespace CatCore.Models.Twitch.IRC
{
	public sealed class TwitchUserState
	{
		// TODO: Look into converting into a dictionary
		[PublicAPI]
		public string? BadgeInfo { get; internal set; }

		// TODO: Look into converting into a dictionary
		[PublicAPI]
		public string? Badges { get; internal set; }

		[PublicAPI]
		public string? Color { get; internal set; }

		[PublicAPI]
		public string UserId { get; internal set; }

		[PublicAPI]
		public string? DisplayName { get; internal set; }

		// TODO: Look into converting into a list
		[PublicAPI]
		public string? EmoteSets { get; internal set; }

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

		public TwitchUserState(string? badgeInfo, string? badges, string? color, string userId, string? displayName, string? emoteSets)
		{
			BadgeInfo = badgeInfo;
			Badges = badges;
			Color = color;
			UserId = userId;
			DisplayName = displayName;
			EmoteSets = emoteSets;

			UpdatePermissions();
		}

		internal void UpdateState(string? badgeInfo, string? badges, string? color, string userId, string? displayName, string? emoteSets)
		{
			BadgeInfo = badgeInfo;
			Badges = badges;
			Color = color;
			UserId = userId;
			DisplayName = displayName;
			EmoteSets = emoteSets;

			UpdatePermissions();
		}

		private void UpdatePermissions()
		{
			if (Badges != null)
			{
				IsModerator = Badges.Contains("moderator/");
				IsBroadcaster = Badges.Contains("broadcaster/");
				IsSubscriber = Badges.Contains("subscriber/") || Badges.Contains("founder/");
				IsTurbo = Badges.Contains("turbo/");
				IsVip = Badges.Contains("vip/");
			}
			else
			{
				IsModerator = false;
				IsBroadcaster = false;
				IsSubscriber = false;
				IsTurbo = false;
				IsVip = false;
			}
		}

		/*badge-info=
		badges=moderator/1
		color=#FF69B4
		display-name=RealEris
		emote-sets=0,300374282,303777092,592920959,610186276
		mod=1
		subscriber=0
		user-type=mod*/
	}
}