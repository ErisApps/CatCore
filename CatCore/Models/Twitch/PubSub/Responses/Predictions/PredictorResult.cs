using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Predictions
{
	public readonly struct PredictorResult
	{
		[JsonPropertyName("type")]
		public string Type { get; }

		[JsonPropertyName("points_won")]
		public uint PointsWon { get; }

		[JsonPropertyName("is_acknowledged")]
		public bool IsAcknowledged { get; }

		[JsonConstructor]
		public PredictorResult(string type, uint pointsWon, bool isAcknowledged)
		{
			Type = type;
			PointsWon = pointsWon;
			IsAcknowledged = isAcknowledged;
		}
	}
}