using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Bans
{
	public readonly struct BannedUserInfo
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; }

		[JsonPropertyName("user_name")]
		public string UserName { get; }

		[JsonPropertyName("expires_at")]
		public string? ExpiresAtRaw { get; }

		public DateTimeOffset? ExpiresAt => DateTimeOffset.TryParse(ExpiresAtRaw, out var parsedValue) ? parsedValue : null;

		[JsonPropertyName("created_at")]
		public DateTimeOffset CreatedAt { get; }

		[JsonPropertyName("reason")]
		public string Reason { get; }

		[JsonPropertyName("moderator_id")]
		public string ModeratorId { get; }

		[JsonPropertyName("moderator_login")]
		public string ModeratorLogin { get; }

		[JsonPropertyName("moderator_name")]
		public string ModeratorName { get; }

		[JsonConstructor]
		public BannedUserInfo(string userId, string userLogin, string userName, string? expiresAtRaw, DateTimeOffset createdAt, string reason, string moderatorId, string moderatorLogin,
			string moderatorName
		)
		{
			UserId = userId;
			UserLogin = userLogin;
			UserName = userName;
			ExpiresAtRaw = expiresAtRaw;
			CreatedAt = createdAt;
			Reason = reason;
			ModeratorId = moderatorId;
			ModeratorLogin = moderatorLogin;
			ModeratorName = moderatorName;
		}
	}
}