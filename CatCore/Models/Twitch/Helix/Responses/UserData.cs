using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct UserData
	{
		[JsonPropertyName("id")]
		public string UserId { get; }

		[JsonPropertyName("login")]
		public string LoginName { get; }

		[JsonPropertyName("display_name")]
		public string DisplayName { get; }

		[JsonPropertyName("description")]
		public string Description { get; }

		[JsonPropertyName("profile_image_url")]
		public string ProfileImageUrl { get; }

		[JsonPropertyName("offline_image_url")]
		public string OfflineImageUrl { get; }

		// User’s type: "staff", "admin", "global_mod", or ""
		[JsonPropertyName("type")]
		public string Type { get; }

		// User’s broadcaster type: "partner", "affiliate", or ""
		[JsonPropertyName("broadcaster_type")]
		public string BroadcasterType { get; }

		[JsonPropertyName("created_at")]
		public DateTimeOffset CreatedAt { get; }

		[JsonPropertyName("view_count")]
		public uint ViewCount { get; }

		/// <remark>
		/// Returned if the request includes the user:read:email scope.
		/// </remark>
		[JsonPropertyName("email")]
		public string Email { get; }

		[JsonConstructor]
		public UserData(string userId, string loginName, string displayName, string description, string profileImageUrl, string offlineImageUrl, string type, string broadcasterType,
			DateTimeOffset createdAt, uint viewCount, string email)
		{
			UserId = userId;
			LoginName = loginName;
			DisplayName = displayName;
			Description = description;
			ProfileImageUrl = profileImageUrl;
			OfflineImageUrl = offlineImageUrl;
			Type = type;
			BroadcasterType = broadcasterType;
			CreatedAt = createdAt;
			ViewCount = viewCount;
			Email = email;
		}
	}
}