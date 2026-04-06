using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Helpers.Converters;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Models.Twitch.PubSub.Responses.Predictions
{
	public readonly struct PredictionData
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("channel_id")]
		public string ChannelId { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("created_at")]
		public DateTime CreatedAt { get; }

		[JsonPropertyName("created_by")]
		public User CreatedBy { get; }

		[JsonPropertyName("ended_at")]
		public DateTime? EndedAt { get; }

		[JsonPropertyName("ended_by")]
		public User? EndedBy { get; }

		[JsonPropertyName("locked_at")]
		public DateTime? LockedAt { get; }

		[JsonPropertyName("locked_by")]
		public User? LockedBy { get; }

		[JsonPropertyName("outcomes")]
		public IReadOnlyList<Outcome> Outcomes { get; }

		[JsonPropertyName("prediction_window_seconds")]
		public uint PredictionWindowSeconds { get; }

		[JsonPropertyName("status")]
		[JsonConverter(typeof(Helpers.Converters.JsonStringEnumConverter<PredictionStatus>))]
		public PredictionStatus Status { get; }

		[JsonPropertyName("winning_outcome_id")]
		public string? WinningOutcomeId { get; }

		[JsonConstructor]
		public PredictionData(string id, string channelId, string title, DateTime createdAt, User createdBy, DateTime? endedAt, User? endedBy, DateTime? lockedAt, User? lockedBy,
			IReadOnlyList<Outcome> outcomes, uint predictionWindowSeconds, PredictionStatus status, string? winningOutcomeId)
		{
			Id = id;
			ChannelId = channelId;
			CreatedAt = createdAt;
			CreatedBy = createdBy;
			EndedAt = endedAt;
			EndedBy = endedBy;
			LockedAt = lockedAt;
			LockedBy = lockedBy;
			Outcomes = outcomes;
			PredictionWindowSeconds = predictionWindowSeconds;
			Status = status;
			Title = title;
			WinningOutcomeId = winningOutcomeId;
		}
	}
}