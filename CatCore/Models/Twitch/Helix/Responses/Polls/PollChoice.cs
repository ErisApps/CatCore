using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Polls
{
	public readonly struct PollChoice
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("votes")]
		public uint Votes { get; }

		[JsonPropertyName("channel_points_votes")]
		public uint ChannelPointsVotes { get; }

		[JsonPropertyName("bits_votes")]
		public uint BitsVotes { get; }

		[JsonConstructor]
		public PollChoice(string id, string title, uint votes, uint channelPointsVotes, uint bitsVotes)
		{
			Id = id;
			Title = title;
			Votes = votes;
			ChannelPointsVotes = channelPointsVotes;
			BitsVotes = bitsVotes;
		}
	}
}