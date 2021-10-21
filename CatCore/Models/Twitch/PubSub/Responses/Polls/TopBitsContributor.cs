using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Polls
{
	public sealed class TopBitsContributor : ContributorBase
	{
		[JsonPropertyName("bits_contributed")]
		public uint BitsContributed { get; }

		public TopBitsContributor(string userId, string displayName, uint bitsContributed) : base(userId, displayName)
		{
			BitsContributed = bitsContributed;
		}
	}
}