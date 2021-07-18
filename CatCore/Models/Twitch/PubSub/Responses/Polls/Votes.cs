using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Polls
{
	public readonly struct Votes
	{
		[JsonPropertyName("total")]
		public uint Total { get; }

		[JsonPropertyName("bits")]
		public uint Bits { get; }

		[JsonPropertyName("channel_points")]
		public uint ChannelPoints { get; }

		[JsonPropertyName("base")]
		public uint Base { get; }

		[JsonConstructor]
		public Votes(uint total, uint bits, uint channelPoints, uint @base)
		{
			Total = total;
			Bits = bits;
			ChannelPoints = channelPoints;
			Base = @base;
		}
	}
}