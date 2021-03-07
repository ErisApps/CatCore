using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Response
{
	public readonly struct UserData
	{
		[JsonPropertyName("id")]
		public string UserId { get; }

		[JsonPropertyName("login")]
		public string LoginName { get; }

		[JsonPropertyName("display_name")]
		public string DisplayName { get; }

		// User’s type: "staff", "admin", "global_mod", or ""
		[JsonPropertyName("type")]
		public string Type { get; }

		// User’s broadcaster type: "partner", "affiliate", or ""
		[JsonPropertyName("broadcaster_type")]
		public string BroadcasterType { get; }

		[JsonPropertyName("created_at")]
		public DateTimeOffset CreatedAt { get; }

		[JsonConstructor]
		public UserData(string userId, string loginName, string displayName, string type, string broadcasterType, DateTimeOffset createdAt)
		{
			UserId = userId;
			LoginName = loginName;
			DisplayName = displayName;
			Type = type;
			BroadcasterType = broadcasterType;
			CreatedAt = createdAt;
		}
	}
}