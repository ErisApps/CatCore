using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Bans
{
	public readonly struct BanUser
	{
		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; }

		[JsonPropertyName("moderator_id")]
		public string ModeratorId { get; }

		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("created_at")]
		public DateTimeOffset CreatedAt { get; }

		[JsonPropertyName("end_time")]
		public DateTimeOffset? EndTime { get; }

		[JsonConstructor]
		public BanUser(string broadcasterId, string moderatorId, string userId, DateTimeOffset createdAt, DateTimeOffset? endTime)
		{
			BroadcasterId = broadcasterId;
			ModeratorId = moderatorId;
			UserId = userId;
			CreatedAt = createdAt;
			EndTime = endTime;
		}
	}
}