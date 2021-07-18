using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests
{
	internal readonly struct CreateStreamMarkerRequestDto
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("description")]
		public string? Description { get; }

		internal CreateStreamMarkerRequestDto(string userId, string? description)
		{
			UserId = userId;
			Description = description;
		}
	}
}