using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Polls
{
	public readonly struct Tokens
	{
		[JsonPropertyName("bits")]
		public uint Bits { get; }

		[JsonPropertyName("channel_points")]
		public uint ChannelPoints { get; }

		public Tokens(uint bits, uint channelPoints)
		{
			Bits = bits;
			ChannelPoints = channelPoints;
		}
	}
}