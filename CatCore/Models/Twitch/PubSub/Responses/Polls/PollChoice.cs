using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Polls
{
	public readonly struct PollChoice
	{
		[JsonPropertyName("choice_id")]
		public string ChoiceId { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("votes")]
		public Votes Votes { get; }

		[JsonPropertyName("tokens")]
		public Tokens Tokens { get; }

		[JsonPropertyName("total_voters")]
		public uint TotalVoters { get; }

		[JsonConstructor]
		public PollChoice(string choiceId, string title, Votes votes, Tokens tokens, uint totalVoters)
		{
			ChoiceId = choiceId;
			Title = title;
			Votes = votes;
			Tokens = tokens;
			TotalVoters = totalVoters;
		}
	}
}