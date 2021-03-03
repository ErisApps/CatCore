using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.OAuth
{
	internal readonly struct ValidationResponse
	{
		[JsonPropertyName("client_id")]
		public string ClientId { get; }

		[JsonPropertyName("login")]
		public string LoginName { get; }

		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("scopes")]
		public string[] Scopes { get; }

		[JsonPropertyName("expires_in")]
		public int ExpiresIn { get; }

		[JsonConstructor]
		public ValidationResponse(string clientId, string loginName, string userId, string[] scopes, int expiresIn)
		{
			ClientId = clientId;
			LoginName = loginName;
			UserId = userId;
			Scopes = scopes;
			ExpiresIn = expiresIn;
		}
	}
}