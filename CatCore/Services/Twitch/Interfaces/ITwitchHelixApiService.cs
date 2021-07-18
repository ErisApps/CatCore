using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Models.Twitch.Helix.Responses.Polls;
using CatCore.Models.Twitch.Helix.Responses.Predictions;
using CatCore.Models.Twitch.Helix.Shared;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchHelixApiService
	{
		Task<ResponseBase<UserData>?> FetchUserInfo(CancellationToken? cancellationToken = null, params string[] loginNames);
		Task<ResponseBase<CreateStreamMarkerData>?> CreateStreamMarker(string userId, string? description = null, CancellationToken? cancellationToken = null);

		Task<ResponseBaseWithPagination<ChannelData>?> SearchChannels(string query, uint? limit = null, bool? liveOnly = null, string? continuationCursor = null,
			CancellationToken? cancellationToken = null);

		Task<ResponseBaseWithPagination<PollData>?> GetPolls(List<string>? pollIds = null, uint? limit = null, string? continuationCursor = null, CancellationToken? cancellationToken = null);

		Task<ResponseBase<PollData>?> CreatePoll(string title, List<string> choices, int duration, bool? bitsVotingEnabled = null, uint? bitsPerVote = null,
			bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null, CancellationToken? cancellationToken = null);

		Task<ResponseBase<PollData>?> EndPoll(string pollId, PollStatus pollStatus, CancellationToken? cancellationToken = null);
		Task<ResponseBaseWithPagination<PredictionData>?> GetPredictions(List<string>? predictionIds = null, uint? limit = null, string? continuationCursor = null, CancellationToken? cancellationToken = null);
	}
}