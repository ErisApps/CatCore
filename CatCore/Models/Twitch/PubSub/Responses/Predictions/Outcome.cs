using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Predictions
{
	public readonly struct Outcome
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("color")]
		public string Color { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("total_points")]
		public uint TotalPoints { get; }

		[JsonPropertyName("total_users")]
		public uint TotalUsers { get; }

		[JsonPropertyName("top_predictors")]
		public IReadOnlyList<TopPredictor> TopPredictors { get; }

		[JsonPropertyName("badge")]
		public Badge Badge { get; }

		[JsonConstructor]
		public Outcome(string id, string color, string title, uint totalPoints, uint totalUsers, IReadOnlyList<TopPredictor> topPredictors, Badge badge)
		{
			Id = id;
			Color = color;
			Title = title;
			TotalPoints = totalPoints;
			TotalUsers = totalUsers;
			TopPredictors = topPredictors;
			Badge = badge;
		}
	}
}