using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Predictions
{
	public readonly struct Outcome
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("users")]
		public uint Users { get; }

		[JsonPropertyName("channel_points")]
		public uint ChannelPoints { get; }

		[JsonPropertyName("top_predictors")]
		public IReadOnlyList<Predictor> TopPredictors { get; }

		[JsonPropertyName("color")]
		public string Color { get; }

		[JsonConstructor]
		public Outcome(string id, string title, uint users, uint channelPoints, IReadOnlyList<Predictor> topPredictors, string color)
		{
			Id = id;
			Title = title;
			Users = users;
			ChannelPoints = channelPoints;
			TopPredictors = topPredictors;
			Color = color;
		}
	}
}