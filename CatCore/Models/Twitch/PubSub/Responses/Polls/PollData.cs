using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Helpers.Converters;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Models.Twitch.PubSub.Responses.Polls
{
	public readonly struct PollData
	{
		[JsonPropertyName("poll_id")]
		public string PollId { get; }

		[JsonPropertyName("owned_by")]
		public string OwnedBy { get; }

		[JsonPropertyName("created_by")]
		public string CreatedBy { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("started_at")]
		public string StartedAtRaw { get; }

		[JsonPropertyName("ended_at")]
		public string EndedAtRaw { get; }

		[JsonPropertyName("ended_by")]
		public object EndedByRaw { get; }

		[JsonPropertyName("duration_seconds")]
		public uint DurationSeconds { get; }

		[JsonPropertyName("settings")]
		public PollSettings Settings { get; }

		[JsonPropertyName("status")]
		[JsonConverter(typeof(Helpers.Converters.JsonStringEnumConverter<PollStatus>))]
		public PollStatus Status { get; }

		[JsonPropertyName("choices")]
		public IReadOnlyList<PollChoice> Choices { get; }

		[JsonPropertyName("votes")]
		public Votes Votes { get; }

		[JsonPropertyName("tokens")]
		public Tokens Tokens { get; }

		[JsonPropertyName("total_voters")]
		public uint TotalVoters { get; }

		[JsonPropertyName("remaining_duration_milliseconds")]
		public uint RemainingDurationMilliseconds { get; }

		[JsonPropertyName("top_contributor")]
		public TopBitsContributor? TopContributor { get; }

		[JsonPropertyName("top_bits_contributor")]
		public TopBitsContributor? TopBitsContributor { get; }

		[JsonPropertyName("top_channel_points_contributor")]
		public TopChannelPointsContributor? TopChannelPointsContributor { get; }

		[JsonConstructor]
		public PollData(string pollId, string ownedBy, string createdBy, string title, string startedAtRaw, string endedAtRaw, object endedByRaw, uint durationSeconds, PollSettings settings,
			PollStatus status, IReadOnlyList<PollChoice> choices, Votes votes, Tokens tokens, uint totalVoters, uint remainingDurationMilliseconds, TopBitsContributor? topContributor,
			TopBitsContributor? topBitsContributor, TopChannelPointsContributor? topChannelPointsContributor)
		{
			PollId = pollId;
			OwnedBy = ownedBy;
			CreatedBy = createdBy;
			Title = title;
			StartedAtRaw = startedAtRaw;
			EndedAtRaw = endedAtRaw;
			EndedByRaw = endedByRaw;
			DurationSeconds = durationSeconds;
			Settings = settings;
			Status = status;
			Choices = choices;
			Votes = votes;
			Tokens = tokens;
			TotalVoters = totalVoters;
			RemainingDurationMilliseconds = remainingDurationMilliseconds;
			TopContributor = topContributor;
			TopBitsContributor = topBitsContributor;
			TopChannelPointsContributor = topChannelPointsContributor;
		}
	}
}