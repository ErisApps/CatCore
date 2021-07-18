using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests.Predictions
{
	internal readonly struct Outcome
	{
		[JsonPropertyName("title")]
		public string Title { get; }

		public Outcome(string title)
		{
			Title = title;
		}
	}
}