using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct StartRaidData
	{
		[JsonPropertyName("created_at")]
		public DateTimeOffset CreatedAt { get; }

		[JsonPropertyName("is_mature")]
		public bool IsMature { get; }

		[JsonConstructor]
		public StartRaidData(DateTimeOffset createdAt, bool isMature)
		{
			CreatedAt = createdAt;
			IsMature = isMature;
		}
	}
}