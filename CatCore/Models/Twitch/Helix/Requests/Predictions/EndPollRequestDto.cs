using System.Text.Json.Serialization;
using CatCore.Helpers.Converters;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Models.Twitch.Helix.Requests.Predictions
{
	internal readonly struct EndPredictionRequestDto
	{
		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; }

		[JsonPropertyName("id")]
		public string PollId { get; }

		[JsonPropertyName("status")]
		[JsonConverter(typeof(Helpers.Converters.JsonStringEnumConverter<PredictionStatus>))]
		public PredictionStatus Status { get; }

		[JsonPropertyName("winning_outcome_id")]
		public string? WinningOutcomeId { get; }

		public EndPredictionRequestDto(string broadcasterId, string pollId, PredictionStatus status, string? winningOutcomeId)
		{
			BroadcasterId = broadcasterId;
			PollId = pollId;
			Status = status;
			WinningOutcomeId = winningOutcomeId;
		}
	}
}