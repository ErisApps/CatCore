using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1
{
	public readonly struct User
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("login")]
		public string Login { get; }

		[JsonPropertyName("display_name")]
		public string DisplayName { get; }

		[JsonConstructor]
		public User(string id, string login, string displayName)
		{
			Id = id;
			Login = login;
			DisplayName = displayName;
		}
	}
}