using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Models.Twitch.Helix.Shared;

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
		public List<PollChoice> Choices { get; }

		[JsonPropertyName("bits_voting_enabled")]
		public bool BitsVotingEnabled { get; }

		[JsonPropertyName("bits_per_vote")]
		public uint BitsPerVote { get; }

		[JsonPropertyName("channel_points_voting_enabled")]
		public bool ChannelPointsVotingEnabled { get; }

		[JsonPropertyName("channel_points_per_vote")]
		public uint ChannelPointsPerVote { get; }

		[JsonPropertyName("status")]
		public string StatusRaw { get; }

		[JsonIgnore]
		public PollStatus Status => Enum.TryParse(StatusRaw, true, out PollStatus pollStatus) ? pollStatus : PollStatus.Invalid;

		[JsonPropertyName("duration")]
		public uint Duration { get; }

		[JsonPropertyName("started_at")]
		public string StartedAtRaw { get; }

		[JsonIgnore]
		public DateTimeOffset StartedAt => DateTimeOffset.Parse(StartedAtRaw);

		[JsonConstructor]
		public PollData(string id, string broadcasterId, string broadcasterName, string broadcasterLogin, string title, List<PollChoice> choices, bool bitsVotingEnabled, uint bitsPerVote,
			bool channelPointsVotingEnabled, uint channelPointsPerVote, string statusRaw, uint duration, string startedAtRaw)
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
			StatusRaw = statusRaw;
			Duration = duration;
			StartedAtRaw = startedAtRaw;
		}
	}
}