using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct CreateStreamMarkerData
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("description")]
		public string? Description { get; }

		[JsonPropertyName("position_seconds")]
		public uint PositionSeconds { get; }

		[JsonPropertyName("created_at")]
		public DateTimeOffset CreatedAt { get; }

		[JsonConstructor]
		public CreateStreamMarkerData(string id, string? description, uint positionSeconds, DateTimeOffset createdAt)
		{
			Id = id;
			Description = description;
			PositionSeconds = positionSeconds;
			CreatedAt = createdAt;
		}
	}
}