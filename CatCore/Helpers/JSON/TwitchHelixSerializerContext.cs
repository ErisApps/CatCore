using System.Text.Json.Serialization;
using HelixRequests = CatCore.Models.Twitch.Helix.Requests;
using HelixResponses = CatCore.Models.Twitch.Helix.Responses;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.UserData>))]
	[JsonSerializable(typeof(HelixRequests.CreateStreamMarkerRequestDto))]
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.CreateStreamMarkerData>))]
	[JsonSerializable(typeof(HelixResponses.ResponseBaseWithPagination<HelixResponses.ChannelData>))]
	[JsonSerializable(typeof(HelixRequests.Polls.PollChoice), TypeInfoPropertyName = "RequestPollChoice")]
	[JsonSerializable(typeof(HelixResponses.Polls.PollChoice), TypeInfoPropertyName = "ResponsePollChoice")]
	[JsonSerializable(typeof(HelixResponses.ResponseBaseWithPagination<HelixResponses.Polls.PollData>))]
	[JsonSerializable(typeof(HelixRequests.Polls.CreatePollRequestDto))]
	[JsonSerializable(typeof(HelixRequests.Polls.EndPollRequestDto))]
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.Polls.PollData>))]
	[JsonSerializable(typeof(HelixRequests.Predictions.Outcome), TypeInfoPropertyName = "RequestOutcome")]
	[JsonSerializable(typeof(HelixResponses.Predictions.Outcome), TypeInfoPropertyName = "ResponseOutcome")]
	[JsonSerializable(typeof(HelixResponses.ResponseBaseWithPagination<HelixResponses.Predictions.PredictionData>))]
	[JsonSerializable(typeof(HelixRequests.Predictions.CreatePredictionsRequestDto))]
	[JsonSerializable(typeof(HelixRequests.Predictions.EndPredictionRequestDto))]
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.Predictions.PredictionData>))]
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.Bits.Cheermotes.CheermoteGroupData>))]
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.Badges.BadgeData>))]
	[JsonSerializable(typeof(HelixResponses.ResponseBaseWithPagination<HelixResponses.Stream>))]
	[JsonSerializable(typeof(HelixResponses.ResponseBaseWithTemplate<HelixResponses.Emotes.GlobalEmote>))]
	[JsonSerializable(typeof(HelixResponses.ResponseBaseWithTemplate<HelixResponses.Emotes.ChannelEmote>))]
	[JsonSerializable(typeof(HelixRequests.ChatSettingsRequestDto))]
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.ChatSettings>))]
	[JsonSerializable(typeof(HelixResponses.ResponseBaseWithPagination<HelixResponses.Bans.BannedUserInfo>))]
	[JsonSerializable(typeof(HelixRequests.LegacyRequestDataWrapper<HelixRequests.Bans.BanUserRequestDto>))]
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.Bans.BanUser>))]
	[JsonSerializable(typeof(HelixRequests.SendChatAnnouncementRequestDto))]
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.UserChatColorData>))]
	[JsonSerializable(typeof(HelixResponses.ResponseBase<HelixResponses.StartRaidData>))]
	internal partial class TwitchHelixSerializerContext : JsonSerializerContext
	{
	}
}