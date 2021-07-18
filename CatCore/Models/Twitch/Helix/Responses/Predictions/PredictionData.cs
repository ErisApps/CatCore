using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Helpers.Converters;
using CatCore.Models.Twitch.Helix.Shared;

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
		[JsonConverter(typeof(JsonStringEnumConverter<PredictionStatus>))]
		public PredictionStatus Status { get; }

		[JsonPropertyName("created_at")]
		public string CreatedAt { get; }

		[JsonPropertyName("ended_at")]
		public string EndedAtRaw { get; }

		[JsonIgnore]
		public DateTimeOffset? EndedAt => DateTimeOffset.TryParse(EndedAtRaw, out var endedAt) ? endedAt : null;

		[JsonPropertyName("locked_at")]
		public string LockedAtRaw { get; }

		[JsonIgnore]
		public DateTimeOffset? LockedAt => DateTimeOffset.TryParse(LockedAtRaw, out var lockedAt) ? lockedAt : null;

		[JsonConstructor]
		public PredictionData(string id, string broadcasterId, string broadcasterName, string broadcasterLogin, string title, string winningOutcomeId, IReadOnlyList<Outcome> outcomes, uint duration,
			PredictionStatus status, string createdAt, string endedAtRaw, string lockedAtRaw)
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
			EndedAtRaw = endedAtRaw;
			LockedAtRaw = lockedAtRaw;
		}
	}
}