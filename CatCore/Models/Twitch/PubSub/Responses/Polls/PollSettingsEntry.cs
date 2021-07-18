using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Polls
{
	public readonly struct PollSettingsEntry
	{
		[JsonPropertyName("is_enabled")]
		public bool IsEnabled { get; }

		[JsonPropertyName("cost")]
		public uint? Cost { get; }

		[JsonConstructor]
		public PollSettingsEntry(bool isEnabled, uint? cost)
		{
			IsEnabled = isEnabled;
			Cost = cost;
		}
	}
}