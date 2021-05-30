using System;
using System.Text.Json.Serialization;

namespace CatCore.Shared.Models.Twitch.OAuth
{
	public readonly struct AuthorizationResponse
	{
		[JsonPropertyName("access_token")]
		public string AccessToken { get; }

		[JsonPropertyName("refresh_token")]
		public string RefreshToken { get; }

		[JsonPropertyName("token_type")]
		public string TokenType { get; }

		[JsonPropertyName("scope")]
		public string[] Scope { get; }

		[JsonPropertyName("expires_in")]
		public int ExpiresInRaw { get; }

		public DateTimeOffset ExpiresIn { get; }

		[JsonConstructor]
		public AuthorizationResponse(string accessToken, string refreshToken, string tokenType, int expiresInRaw, string[] scope)
		{
			AccessToken = accessToken;
			RefreshToken = refreshToken;
			TokenType = tokenType;
			Scope = scope;
			ExpiresInRaw = expiresInRaw;
			ExpiresIn = DateTimeOffset.Now.AddSeconds(expiresInRaw);
		}
	}
}