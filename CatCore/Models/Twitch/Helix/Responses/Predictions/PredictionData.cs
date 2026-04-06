using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Helpers.Converters;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Models.Twitch.Helix.Responses.Predictions
{
	public readonly struct PredictionData
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; }

		[JsonPropertyName("broadcaster_name")]
		public string BroadcasterName { get; }

		[JsonPropertyName("broadcaster_login")]
		public string BroadcasterLogin { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("winning_outcome_id")]
		public string WinningOutcomeId { get; }

		[JsonPropertyName("outcomes")]
		public IReadOnlyList<Outcome> Outcomes { get; }

		[JsonPropertyName("prediction_window")]
		public uint Duration { get; }

		[JsonPropertyName("status")]
		[JsonConverter(typeof(Helpers.Converters.JsonStringEnumConverter<PredictionStatus>))]
		public PredictionStatus Status { get; }

		[JsonPropertyName("created_at")]
		public DateTimeOffset CreatedAt { get; }

		[JsonPropertyName("ended_at")]
		public DateTimeOffset? EndedAt { get; }

		[JsonPropertyName("locked_at")]
		public DateTimeOffset? LockedAt { get; }

		[JsonConstructor]
		public PredictionData(string id, string broadcasterId, string broadcasterName, string broadcasterLogin, string title, string winningOutcomeId, IReadOnlyList<Outcome> outcomes, uint duration,
			PredictionStatus status, DateTimeOffset createdAt, DateTimeOffset? endedAt, DateTimeOffset? lockedAt)
		{
			Id = id;
			BroadcasterId = broadcasterId;
			BroadcasterName = broadcasterName;
			BroadcasterLogin = broadcasterLogin;
			Title = title;
			WinningOutcomeId = winningOutcomeId;
			Outcomes = outcomes;
			Duration = duration;
			Status = status;
			CreatedAt = createdAt;
			EndedAt = endedAt;
			LockedAt = lockedAt;
		}
	}
}