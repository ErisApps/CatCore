using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct UserChatColorData
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("user_name")]
		public string UserName { get; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; }

		[JsonPropertyName("color")]
		public string Color { get; }

		[JsonConstructor]
		public UserChatColorData(string userId, string userName, string userLogin, string color)
		{
			UserId = userId;
			UserName = userName;
			UserLogin = userLogin;
			Color = color;
		}
	}
}