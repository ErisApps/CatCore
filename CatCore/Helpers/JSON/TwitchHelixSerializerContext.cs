using System.Text.Json.Serialization;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Models.Twitch.Helix.Responses.Badges;
using CatCore.Models.Twitch.Helix.Responses.Bans;
using CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes;
using CatCore.Models.Twitch.Helix.Responses.Emotes;
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
	[JsonSerializable(typeof(ResponseBase<BadgeData>))]
	[JsonSerializable(typeof(ResponseBaseWithPagination<Stream>))]
	[JsonSerializable(typeof(ResponseBaseWithTemplate<GlobalEmote>))]
	[JsonSerializable(typeof(ResponseBaseWithTemplate<ChannelEmote>))]
	[JsonSerializable(typeof(ResponseBase<ChatSettings>))]
	[JsonSerializable(typeof(ResponseBaseWithPagination<BannedUserInfo>))]
	[JsonSerializable(typeof(ResponseBase<BanUser>))]
	[JsonSerializable(typeof(ResponseBase<UserChatColorData>))]
	[JsonSerializable(typeof(ResponseBase<StartRaidData>))]
	internal partial class TwitchHelixSerializerContext : JsonSerializerContext
	{
	}
}