using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Predictions
{
	public readonly struct TopPredictor
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("event_id")]
		public string EventId { get; }

		[JsonPropertyName("outcome_id")]
		public string OutcomeId { get; }

		[JsonPropertyName("channel_id")]
		public string ChannelId { get; }

		[JsonPropertyName("points")]
		public uint Points { get; }

		[JsonPropertyName("predicted_at")]
		public DateTime PredictedAt { get; }

		[JsonPropertyName("updated_at")]
		public DateTime UpdatedAt { get; }

		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("result")]
		public PredictorResult Result { get; }

		[JsonPropertyName("user_display_name")]
		public string DisplayName { get; }

		public TopPredictor(string id, string eventId, string outcomeId, string channelId, uint points, DateTime predictedAt, DateTime updatedAt, string userId, PredictorResult result, string displayName)
		{
			Id = id;
			EventId = eventId;
			OutcomeId = outcomeId;
			ChannelId = channelId;
			Points = points;
			PredictedAt = predictedAt;
			UpdatedAt = updatedAt;
			UserId = userId;
			Result = result;
			DisplayName = displayName;
		}
	}
}