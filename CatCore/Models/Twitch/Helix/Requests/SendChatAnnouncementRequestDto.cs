using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests
{
	internal readonly struct SendChatAnnouncementRequestDto
	{
		[JsonPropertyName("message")]
		public string Message { get; }

		[JsonPropertyName("color")]
		public string Color { get; }

		public SendChatAnnouncementRequestDto(string message, SendChatAnnouncementColor color)
		{
			Message = message;
			Color = color switch
			{
				SendChatAnnouncementColor.Primary => "primary",
				SendChatAnnouncementColor.Blue => "blue",
				SendChatAnnouncementColor.Green => "green",
				SendChatAnnouncementColor.Orange => "orange",
				SendChatAnnouncementColor.Purple => "purple",
				_ => throw new ArgumentOutOfRangeException(nameof(color), color, "An invalid color was provided.")
			};
		}
	}
}