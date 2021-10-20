using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1
{
	public readonly struct MaxPerStream
	{
		[JsonPropertyName("is_enabled")]
		public bool Enabled { get; }

		[JsonPropertyName("max_per_stream")]
		public uint MaxCount { get; }

		[JsonConstructor]
		public MaxPerStream(bool enabled, uint maxCount)
		{
			Enabled = enabled;
			MaxCount = maxCount;
		}
	}
}