using System.Text.Json.Serialization;
using CatCore.Models.Twitch.PubSub.Requests;
using CatCore.Models.Twitch.PubSub.Responses;
using CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1;
using CatCore.Models.Twitch.PubSub.Responses.Polls;
using CatCore.Models.Twitch.PubSub.Responses.Predictions;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(TopicNegotiationMessage))]
	[JsonSerializable(typeof(Follow))]
	[JsonSerializable(typeof(PollData))]
	[JsonSerializable(typeof(PredictionData))]
	[JsonSerializable(typeof(Models.Twitch.PubSub.Responses.Predictions.User), TypeInfoPropertyName = "PredictionUser")]
	[JsonSerializable(typeof(RewardRedeemedData))]
	[JsonSerializable(typeof(Models.Twitch.PubSub.Responses.ChannelPointsChannelV1.User), TypeInfoPropertyName = "RewardRedeemedUser")]
	internal partial class TwitchPubSubSerializerContext : JsonSerializerContext
	{
	}
}