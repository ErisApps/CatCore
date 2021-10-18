using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Polls
{
	public abstract class ContributorBase
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("display_name")]
		public string DisplayName { get; }

		protected ContributorBase(string userId, string displayName)
		{
			UserId = userId;
			DisplayName = displayName;
		}
	}
}