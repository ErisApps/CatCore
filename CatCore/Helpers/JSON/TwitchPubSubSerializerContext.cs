using System.Text.Json.Serialization;
using CatCore.Models.Twitch.PubSub.Requests;
using CatCore.Models.Twitch.PubSub.Responses;
using CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1;
using CatCore.Models.Twitch.PubSub.Responses.Polls;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(TopicNegotiationMessage))]
	[JsonSerializable(typeof(Follow))]
	[JsonSerializable(typeof(PollData))]
	[JsonSerializable(typeof(RewardRedeemedData))]
	internal partial class TwitchPubSubSerializerContext : JsonSerializerContext
	{
	}
}