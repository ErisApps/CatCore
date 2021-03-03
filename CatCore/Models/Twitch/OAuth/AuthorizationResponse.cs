using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.OAuth
{
	internal readonly struct AuthorizationResponse
	{
		[JsonPropertyName("access_token")]
		public string AccessToken { get; }

		[JsonPropertyName("refresh_toekn")]
		public string RefreshToken { get; }

		[JsonPropertyName("token_type")]
		public string TokenType { get; }

		[JsonPropertyName("expires_in")]
		public int ExpiresIn { get; }

		[JsonPropertyName("scope")]
		public string[] Scope { get; }

		[JsonConstructor]
		public AuthorizationResponse(string accessToken, string refreshToken, string tokenType, int expiresIn, string[] scope)
		{
			AccessToken = accessToken;
			RefreshToken = refreshToken;
			TokenType = tokenType;
			ExpiresIn = expiresIn;
			Scope = scope;
		}
	}
}