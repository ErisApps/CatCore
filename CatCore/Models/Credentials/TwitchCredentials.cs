using System;
using System.Text.Json.Serialization;
using CatCore.Shared.Models.Twitch.OAuth;

namespace CatCore.Models.Credentials
{
	internal sealed class TwitchCredentials : ICredentials, IEquatable<TwitchCredentials>
	{
		public string? AccessToken { get; }
		public string? RefreshToken { get; }
		public DateTimeOffset? ValidUntil { get; }

		public TwitchCredentials()
		{
		}

		[JsonConstructor]
		public TwitchCredentials(string? accessToken, string? refreshToken, DateTimeOffset? validUntil)
		{
			AccessToken = accessToken;
			RefreshToken = refreshToken;
			ValidUntil = validUntil;
		}

		public TwitchCredentials(AuthorizationResponse authorizationResponse)
		{
			AccessToken = authorizationResponse.AccessToken;
			RefreshToken = authorizationResponse.RefreshToken;
			ValidUntil = authorizationResponse.ExpiresIn;
		}

		public static TwitchCredentials Empty() => new();

		public bool Equals(TwitchCredentials? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return AccessToken == other.AccessToken && RefreshToken == other.RefreshToken;
		}

		public override bool Equals(object? obj)
		{
			return ReferenceEquals(this, obj) || obj is TwitchCredentials other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (AccessToken != null ? AccessToken.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (RefreshToken != null ? RefreshToken.GetHashCode() : 0);
				return hashCode;
			}
		}
	}
}