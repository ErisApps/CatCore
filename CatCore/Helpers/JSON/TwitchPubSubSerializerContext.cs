using System.Text.Json.Serialization;
using CatCore.Models.Twitch.PubSub.Requests;
using CatCore.Models.Twitch.PubSub.Responses;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(TopicNegotiationMessage))]
	[JsonSerializable(typeof(Follow))]
	internal partial class TwitchPubSubSerializerContext : JsonSerializerContext
	{
	}
}