using System.Text.Json.Serialization;
using CatCore.Models.Twitch.Helix.Requests;
using CatCore.Models.Twitch.Helix.Requests.Bans;
using CatCore.Models.Twitch.Helix.Requests.Polls;
using CatCore.Models.Twitch.Helix.Requests.Predictions;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Models.Twitch.Helix.Responses.Badges;
using CatCore.Models.Twitch.Helix.Responses.Bans;
using CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes;
using CatCore.Models.Twitch.Helix.Responses.Emotes;
using CatCore.Models.Twitch.Helix.Responses.Polls;
using CatCore.Models.Twitch.Helix.Responses.Predictions;
using HelixRequests = CatCore.Models.Twitch.Helix.Requests;
using HelixResponses = CatCore.Models.Twitch.Helix.Responses;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(ResponseBase<UserData>))]
	[JsonSerializable(typeof(CreateStreamMarkerRequestDto))]
	[JsonSerializable(typeof(ResponseBase<CreateStreamMarkerData>))]
	[JsonSerializable(typeof(ResponseBaseWithPagination<ChannelData>))]
	[JsonSerializable(typeof(HelixRequests.Polls.PollChoice), TypeInfoPropertyName = "RequestPollChoice")]
	[JsonSerializable(typeof(HelixResponses.Polls.PollChoice), TypeInfoPropertyName = "ResponsePollChoice")]
	[JsonSerializable(typeof(ResponseBaseWithPagination<PollData>))]
	[JsonSerializable(typeof(CreatePollRequestDto))]
	[JsonSerializable(typeof(EndPollRequestDto))]
	[JsonSerializable(typeof(ResponseBase<PollData>))]
	[JsonSerializable(typeof(HelixRequests.Predictions.Outcome), TypeInfoPropertyName = "RequestOutcome")]
	[JsonSerializable(typeof(HelixResponses.Predictions.Outcome), TypeInfoPropertyName = "ResponseOutcome")]
	[JsonSerializable(typeof(ResponseBaseWithPagination<PredictionData>))]
	[JsonSerializable(typeof(CreatePredictionsRequestDto))]
	[JsonSerializable(typeof(EndPredictionRequestDto))]
	[JsonSerializable(typeof(ResponseBase<PredictionData>))]
	[JsonSerializable(typeof(ResponseBase<CheermoteGroupData>))]
	[JsonSerializable(typeof(ResponseBase<BadgeData>))]
	[JsonSerializable(typeof(ResponseBaseWithPagination<Stream>))]
	[JsonSerializable(typeof(ResponseBaseWithTemplate<GlobalEmote>))]
	[JsonSerializable(typeof(ResponseBaseWithTemplate<ChannelEmote>))]
	[JsonSerializable(typeof(ChatSettingsRequestDto))]
	[JsonSerializable(typeof(ResponseBase<ChatSettings>))]
	[JsonSerializable(typeof(ResponseBaseWithPagination<BannedUserInfo>))]
	[JsonSerializable(typeof(LegacyRequestDataWrapper<BanUserRequestDto>))]
	[JsonSerializable(typeof(ResponseBase<BanUser>))]
	[JsonSerializable(typeof(SendChatAnnouncementRequestDto))]
	[JsonSerializable(typeof(ResponseBase<UserChatColorData>))]
	[JsonSerializable(typeof(ResponseBase<StartRaidData>))]
	internal partial class TwitchHelixSerializerContext : JsonSerializerContext
	{
	}
}