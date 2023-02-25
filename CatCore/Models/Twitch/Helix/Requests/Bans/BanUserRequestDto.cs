using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests.Bans
{
	internal readonly struct BanUserRequestDto
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("duration")]
		public uint? Duration { get; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("reason")]
		public string? Reason { get; }

		public BanUserRequestDto(string userId, uint? duration, string? reason)
		{
			UserId = userId;
			Duration = duration;
			Reason = reason;
		}
	}
}