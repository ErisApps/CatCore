using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests.Polls
{
	internal readonly struct CreatePollRequestDto
	{
		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("choices")]
		public List<PollChoice> Choices { get; }

		[JsonPropertyName("duration")]
		public uint Duration { get; }

		[JsonPropertyName("bits_voting_enabled")]
		public bool? BitsVotingEnabled { get; }

		[JsonPropertyName("bits_per_vote")]
		public uint? BitsPerVote { get; }

		[JsonPropertyName("channel_points_voting_enabled")]
		public bool? ChannelPointsVotingEnabled { get; }

		[JsonPropertyName("channel_points_per_vote")]
		public uint? ChannelPointsPerVote { get; }

		public CreatePollRequestDto(string broadcasterId, string title, List<PollChoice> choices, uint duration, bool? bitsVotingEnabled = null, uint? bitsPerVote = null,
			bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null)
		{
			BroadcasterId = broadcasterId;
			Title = title;
			Choices = choices;
			Duration = duration;
			BitsVotingEnabled = bitsVotingEnabled;
			BitsPerVote = bitsPerVote;
			ChannelPointsVotingEnabled = channelPointsVotingEnabled;
			ChannelPointsPerVote = channelPointsPerVote;
		}
	}
}