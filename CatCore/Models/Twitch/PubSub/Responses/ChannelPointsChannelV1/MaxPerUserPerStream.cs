using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1
{
	public readonly struct MaxPerUserPerStream
	{
		[JsonPropertyName("is_enabled")]
		public bool Enabled { get; }

		[JsonPropertyName("max_per_user_per_stream")]
		public uint MaxCount { get; }

		[JsonConstructor]
		public MaxPerUserPerStream(bool enabled, uint maxCount)
		{
			Enabled = enabled;
			MaxCount = maxCount;
		}
	}
}