using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Polls
{
	public sealed class TopChannelPointsContributor : ContributorBase
	{
		[JsonPropertyName("channel_points_contributed")]
		public uint ChannelPointsContributed { get; }

		[JsonConstructor]
		public TopChannelPointsContributor(string userId, string displayName, uint channelPointsContributed) : base(userId, displayName)
		{
			ChannelPointsContributed = channelPointsContributed;
		}
	}
}