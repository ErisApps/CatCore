using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.PubSub.Responses
{
	[PublicAPI]
	public readonly struct Follow
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("username")]
		public string Username { get; }

		[JsonPropertyName("display_name")]
		public string DisplayName { get; }

		[JsonConstructor]
		public Follow(string userId, string username, string displayName)
		{
			UserId = userId;
			Username = username;
			DisplayName = displayName;
		}
	}
}