using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Helpers.Converters;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Models.Twitch.Helix.Responses.Polls
{
	public readonly struct PollData
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

		[JsonPropertyName("choices")]
		public IReadOnlyList<PollChoice> Choices { get; }

		[JsonPropertyName("bits_voting_enabled")]
		public bool BitsVotingEnabled { get; }

		[JsonPropertyName("bits_per_vote")]
		public uint BitsPerVote { get; }

		[JsonPropertyName("channel_points_voting_enabled")]
		public bool ChannelPointsVotingEnabled { get; }

		[JsonPropertyName("channel_points_per_vote")]
		public uint ChannelPointsPerVote { get; }

		[JsonPropertyName("status")]
		[JsonConverter(typeof(Helpers.Converters.JsonStringEnumConverter<PollStatus>))]
		public PollStatus Status { get; }

		[JsonPropertyName("duration")]
		public uint Duration { get; }

		[JsonPropertyName("started_at")]
		public DateTimeOffset StartedAt { get; }

		[JsonPropertyName("ended_at")]
		public DateTimeOffset? EndedAt { get; }

		[JsonConstructor]
		public PollData(string id, string broadcasterId, string broadcasterName, string broadcasterLogin, string title, IReadOnlyList<PollChoice> choices, bool bitsVotingEnabled, uint bitsPerVote,
			bool channelPointsVotingEnabled, uint channelPointsPerVote, PollStatus status, uint duration, DateTimeOffset startedAt, DateTimeOffset? endedAt)
		{
			Id = id;
			BroadcasterId = broadcasterId;
			BroadcasterName = broadcasterName;
			BroadcasterLogin = broadcasterLogin;
			Title = title;
			Choices = choices;
			BitsVotingEnabled = bitsVotingEnabled;
			BitsPerVote = bitsPerVote;
			ChannelPointsVotingEnabled = channelPointsVotingEnabled;
			ChannelPointsPerVote = channelPointsPerVote;
			Status = status;
			Duration = duration;
			StartedAt = startedAt;
			EndedAt = endedAt;
		}
	}
}