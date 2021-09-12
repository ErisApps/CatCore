using JetBrains.Annotations;

namespace CatCore.Models.Twitch.IRC
{
	public sealed class TwitchGlobalUserState
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
		public string EmoteSets { get; internal set; }

		public TwitchGlobalUserState(string? badgeInfo, string? badges, string? color, string userId, string? displayName, string emoteSets)
		{
			BadgeInfo = badgeInfo;
			Badges = badges;
			Color = color;
			UserId = userId;
			DisplayName = displayName;
			EmoteSets = emoteSets;
		}

		/*badge-info=
		badges=
		color=#FF69B4
		display-name=RealEris
		emote-sets=0,300374282,303777092,592920959,610186276
		user-id=405499635
		user-type=*/
	}
}