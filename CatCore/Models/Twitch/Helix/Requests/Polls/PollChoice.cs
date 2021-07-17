using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests.Polls
{
	internal readonly struct PollChoice
	{
		[JsonPropertyName("title")]
		public string Title { get; }

		public PollChoice(string title)
		{
			Title = title;
		}
	}
}