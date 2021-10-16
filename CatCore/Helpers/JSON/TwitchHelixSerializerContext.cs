using System.Text.Json.Serialization;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes;
using CatCore.Models.Twitch.Helix.Responses.Polls;
using CatCore.Models.Twitch.Helix.Responses.Predictions;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(ResponseBase<UserData>))]
	[JsonSerializable(typeof(ResponseBase<CreateStreamMarkerData>))]
	[JsonSerializable(typeof(ResponseBaseWithPagination<ChannelData>))]
	[JsonSerializable(typeof(ResponseBase<PollData>))]
	[JsonSerializable(typeof(ResponseBaseWithPagination<PollData>))]
	[JsonSerializable(typeof(ResponseBase<PredictionData>))]
	[JsonSerializable(typeof(ResponseBaseWithPagination<PredictionData>))]
	[JsonSerializable(typeof(ResponseBase<CheermoteGroupData>))]
	internal partial class TwitchHelixSerializerContext : JsonSerializerContext
	{
	}
}