namespace CatCore.Models.Twitch.PubSub.Responses
{
	public readonly struct Follow
	{
		public string UserId { get; }
		public string Username { get; }
		public string DisplayName { get; }

		public Follow(string userId, string username, string displayName)
		{
			UserId = userId;
			Username = username;
			DisplayName = displayName;
		}
	}
}