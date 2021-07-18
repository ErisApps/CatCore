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
		public int Duration { get; }

		public CreatePredictionsRequestDto(string broadcasterId, string title, List<Outcome> choices, int duration)
		{
			BroadcasterId = broadcasterId;
			Title = title;
			Choices = choices;
			Duration = duration;
		}
	}
}