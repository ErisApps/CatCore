using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests.Predictions
{
	internal readonly struct CreatePredictionsRequestDto
	{
		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("outcomes")]
		public List<Outcome> Choices { get; }

		[JsonPropertyName("prediction_window")]
		public uint Duration { get; }

		public CreatePredictionsRequestDto(string broadcasterId, string title, List<Outcome> choices, uint duration)
		{
			BroadcasterId = broadcasterId;
			Title = title;
			Choices = choices;
			Duration = duration;
		}
	}
}