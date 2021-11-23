using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Predictions
{
	public readonly struct User
	{
		[JsonPropertyName("type")]
		public string Type { get; }

		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("user_display_name")]
		public string UserDisplayName { get; }

		[JsonPropertyName("extension_client_id")]
		public string? ExtensionClientId { get; }

		[JsonConstructor]
		public User(string type, string userId, string userDisplayName, string? extensionClientId)
		{
			Type = type;
			UserId = userId;
			UserDisplayName = userDisplayName;
			ExtensionClientId = extensionClientId;
		}
	}
}