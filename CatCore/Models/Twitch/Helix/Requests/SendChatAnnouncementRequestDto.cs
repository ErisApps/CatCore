using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests
{
	internal readonly struct SendChatAnnouncementRequestDto
	{
		[JsonPropertyName("message")]
		public string Message { get; }

		[JsonPropertyName("color")]
		public string Color { get; }

		[JsonConstructor]
		public SendChatAnnouncementRequestDto(string message, string color)
		{
			Message = message;
			Color = color;
		}
	}
}