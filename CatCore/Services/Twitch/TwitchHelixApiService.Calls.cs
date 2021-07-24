using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Models.Twitch.Helix.Requests;
using CatCore.Models.Twitch.Helix.Requests.Polls;
using CatCore.Models.Twitch.Helix.Requests.Predictions;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Models.Twitch.Helix.Responses.Polls;
using CatCore.Models.Twitch.Helix.Responses.Predictions;
using CatCore.Models.Twitch.Shared;
using Outcome = CatCore.Models.Twitch.Helix.Requests.Predictions.Outcome;
using PollChoice = CatCore.Models.Twitch.Helix.Requests.Polls.PollChoice;

namespace CatCore.Services.Twitch
{
	public partial class TwitchHelixApiService
	{
		/// <summary>
		/// Gets information about one or more specified Twitch users.
		/// Users are identified by optional user IDs and/or login name. If neither a user ID nor a login name is specified, the user is looked up by Bearer token.
		/// </summary>
		/// <param name="userIds">List of ids of the users for which you want to request data</param>
		/// <param name="loginNames">List of login names of the users for which you want to request data</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing userdata</returns>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-users">Check out the Twitch API Reference docs.</a></remarks>
		public Task<ResponseBase<UserData>?> FetchUserInfo(string[]? userIds = null, string[]? loginNames = null, CancellationToken? cancellationToken = null)
		{
			var uriBuilder = new StringBuilder($"{TWITCH_HELIX_BASEURL}users");

			var totalParamCount = 0;
			void CheckCount(ref string[]? array, out bool hasBool)
			{
				if (array != null)
				{
					totalParamCount += array.Length;
					hasBool = array.Length > 0;
				}
				else
				{
					hasBool = false;
				}
			}

			CheckCount(ref userIds, out var hasUserIds);
			CheckCount(ref loginNames, out var hasLoginNames);

			if (totalParamCount > 100)
			{
				throw new ArgumentException("The userIds and loginNames arguments may have no more than 100 entries combined by Helix. Please ensure that you're requesting 100 tops.",
					$"{nameof(userIds)} | {nameof(loginNames)}");
			}

			if (hasUserIds || hasLoginNames)
			{
				uriBuilder.Append("?");
			}

			if (hasUserIds)
			{
				uriBuilder.Append("id=").Append(string.Join("&id=", userIds!));
			}

			if (hasLoginNames)
			{
				uriBuilder.Append("login=").Append(string.Join("&login=", loginNames!));
			}

			return GetAsync<ResponseBase<UserData>>(uriBuilder.ToString(), cancellationToken);
		}

		/// <summary>
		/// Creates a marker in the stream of a user specified by user ID.
		/// A marker is an arbitrary point in a stream that the broadcaster wants to mark; e.g., to easily return to later.
		/// The marker is created at the current timestamp in the live broadcast when the request is processed.
		/// Markers can be created by the stream owner or editors.
		/// </summary>
		/// <param name="userId">ID of the broadcaster in whose live stream the marker is created.</param>
		/// <param name="description">Description of or comments on the marker. Max length is 140 characters.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data regarding the created stream marker.</returns>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#create-stream-marker">Check out the Twitch API Reference docs.</a></remarks>
		public Task<ResponseBase<CreateStreamMarkerData>?> CreateStreamMarker(string userId, string? description = null, CancellationToken? cancellationToken = null)
		{
			if (!string.IsNullOrWhiteSpace(description) && description!.Length > 140)
			{
				throw new ArgumentException("The description argument is enforced to be 140 characters tops by Helix. Please use a shorter one.", nameof(description));
			}

			var body = new CreateStreamMarkerRequestDto(userId, description);
			return PostAsync<ResponseBase<CreateStreamMarkerData>, CreateStreamMarkerRequestDto>($"{TWITCH_HELIX_BASEURL}streams/markers", body, cancellationToken);
		}

		/// <summary>
		/// Returns a list of channels (users who have streamed within the past 6 months) that match the query via channel name or description either entirely or partially.
		/// Results include both live and offline channels. Online channels will have additional metadata (e.g. StartedAt, TagIds).
		/// </summary>
		/// <param name="query">Query used to search channels</param>
		/// <param name="limit">Maximum number of results to return. Maximum: 100 Default: 20</param>
		/// <param name="liveOnly">Filter results for live streams only</param>
		/// <param name="continuationCursor">Cursor for forward pagination: tells the server where to start fetching the next set of results, in a multi-page response</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing channels matching the provided query</returns>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#search-channels">Check out the Twitch API Reference docs.</a></remarks>
		public Task<ResponseBaseWithPagination<ChannelData>?> SearchChannels(string query, uint? limit = null, bool? liveOnly = null, string? continuationCursor = null,
			CancellationToken? cancellationToken = null)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				throw new ArgumentException("The query parameter should not be null, empty or whitespace.", nameof(query));
			}

			var urlBuilder = new StringBuilder($"{TWITCH_HELIX_BASEURL}search/channels?query={query}");
			if (limit != null)
			{
				if (limit.Value > 100)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 100.", nameof(limit));
				}

				urlBuilder.Append($"&first={limit}");
			}

			if (liveOnly != null)
			{
				urlBuilder.Append($"&live_only={liveOnly}");
			}

			if (continuationCursor != null)
			{
				if (string.IsNullOrWhiteSpace(continuationCursor))
				{
					throw new ArgumentException("The continuationCursor parameter should not be null, empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append($"&after={continuationCursor}");
			}

			return GetAsync<ResponseBaseWithPagination<ChannelData>>(urlBuilder.ToString(), cancellationToken);
		}

		/// <summary>
		/// Get information about all polls or specific polls for a Twitch channel. Poll information is available for 90 days.
		/// </summary>
		/// <param name="pollIds">Filters results to one or more specific polls. Not providing one or more IDs will return the full list of polls for the authenticated channel. Maximum: 100</param>
		/// <param name="limit">Maximum number of results to return. Maximum: 20 Default: 20</param>
		/// <param name="continuationCursor">Cursor for forward pagination: tells the server where to start fetching the next set of results, in a multi-page response</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of 0, 1 or more more polls</returns>
		/// <exception cref="Exception">Gets thrown when the user isn't logged in.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-polls">Check out the Twitch API Reference docs.</a></remarks>
		// ReSharper disable once CognitiveComplexity
		public Task<ResponseBaseWithPagination<PollData>?> GetPolls(List<string>? pollIds = null, uint? limit = null, string? continuationCursor = null, CancellationToken? cancellationToken = null)
		{
			var loggedInUser = _twitchAuthService.LoggedInUser;
			if (loggedInUser == null)
			{
				throw new Exception("The user wasn't logged in yet. Try again later.");
			}

			var urlBuilder = new StringBuilder($"{TWITCH_HELIX_BASEURL}polls?broadcaster_id={loggedInUser.Value.UserId}");
			if (pollIds != null && pollIds.Any())
			{
				if (pollIds.Count > 100)
				{
					throw new ArgumentException("The pollIds parameter has an upper-limit of 100.", nameof(pollIds));
				}

				foreach (var pollId in pollIds)
				{
					urlBuilder.Append($"&id={pollId}");
				}
			}

			if (limit != null)
			{
				if (limit.Value > 20)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 20.", nameof(limit));
				}

				urlBuilder.Append($"&first={limit}");
			}

			if (continuationCursor != null)
			{
				if (string.IsNullOrWhiteSpace(continuationCursor))
				{
					throw new ArgumentException("The continuationCursor parameter should not be null, empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append($"&after={continuationCursor}");
			}

			return GetAsync<ResponseBaseWithPagination<PollData>>(urlBuilder.ToString(), cancellationToken);
		}

		/// <summary>
		/// Create a poll for a specific Twitch channel.
		/// </summary>
		/// <param name="title">Question displayed for the poll. Maximum: 60 characters.</param>
		/// <param name="choices">Array of possible poll choices. Minimum: 2 choices. Maximum: 5 choices.</param>
		/// <param name="duration">Total duration for the poll (in seconds). Minimum: 15. Maximum: 1800.</param>
		/// <param name="bitsVotingEnabled">Indicates if Bits can be used for voting.</param>
		/// <param name="bitsPerVote">Number of Bits required to vote once with Bits. Minimum: 1. Maximum: 10000.</param>
		/// <param name="channelPointsVotingEnabled">Indicates if Channel Points can be used for voting.</param>
		/// <param name="channelPointsPerVote">Number of Channel Points required to vote once with Channel Points. Minimum: 1. Maximum: 1000000.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of the newly created poll</returns>
		/// <exception cref="Exception">Gets thrown when the user isn't logged in.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#create-poll">Check out the Twitch API Reference docs.</a></remarks>
		// ReSharper disable once CognitiveComplexity
		public Task<ResponseBase<PollData>?> CreatePoll(string title, List<string> choices, int duration, bool? bitsVotingEnabled = null, uint? bitsPerVote = null,
			bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null, CancellationToken? cancellationToken = null)
		{
			var loggedInUser = _twitchAuthService.LoggedInUser;
			if (loggedInUser == null)
			{
				throw new Exception("The user wasn't logged in yet. Try again later.");
			}

			var userId = loggedInUser.Value.UserId;

			if (string.IsNullOrWhiteSpace(title) || title.Length > 60)
			{
				throw new ArgumentException("The title argument is enforced to be 60 characters tops by Helix. Please use a shorter one.", nameof(title));
			}

			if (choices.Count is <2 or >5)
			{
				throw new ArgumentException(
					"The choices argument is enforced to have minimum 2 and maximum 5 choices by Helix. Please ensure that the amount of provided choices satisfies this range.", nameof(choices));
			}

			var pollChoices = new List<PollChoice>();
			foreach (var choice in choices)
			{
				if (string.IsNullOrWhiteSpace(choice) || choice.Length > 25)
				{
					throw new ArgumentException(
						"The choices argument is enforced to have all its entries be 25 characters tops by Helix. Please make sure that all entries are 25 characters long or shorter.",
						nameof(choices));
				}

				pollChoices.Add(new PollChoice(choice));
			}

			if (duration is < 15 or > 1800)
			{
				throw new ArgumentException(
					"The duration argument is enforced to last at least 15 seconds and 1800 seconds tops by Helix. Please make sure that all entries are 25 characters long or shorter.",
					nameof(duration));
			}

			void OptionalParametersValidation(ref bool? voteEnabled, ref uint? costPerVote, uint costThreshold)
			{
				if (voteEnabled != null)
				{
					if (voteEnabled.Value)
					{
						if (costPerVote == null)
						{
							costPerVote = 1;
						}
						else if (costPerVote > costThreshold)
						{
							costPerVote = costThreshold;
						}
					}
					else
					{
						voteEnabled = null;
						costPerVote = null;
					}
				}
				else
				{
					costPerVote = null;
				}
			}

			OptionalParametersValidation(ref bitsVotingEnabled, ref bitsPerVote, 10000);
			OptionalParametersValidation(ref channelPointsVotingEnabled, ref channelPointsPerVote, 1000000);

			var body = new CreatePollRequestDto(userId, title, pollChoices, duration, bitsVotingEnabled, bitsPerVote, channelPointsVotingEnabled, channelPointsPerVote);
			return PostAsync<ResponseBase<PollData>, CreatePollRequestDto>($"{TWITCH_HELIX_BASEURL}polls", body, cancellationToken);
		}

		/// <summary>
		/// End a poll that is currently active.
		/// </summary>
		/// <param name="pollId">Id of the poll.</param>
		/// <param name="pollStatus">The poll status to be set. Valid values:
		/// <list type="bullet">
		/// <item><description><see cref="PollStatus.Terminated"/>: End the poll manually, but allow it to be viewed publicly.</description></item>
		/// <item><description><see cref="PollStatus.Archived"/>: End the poll manually and do not allow it to be viewed publicly.</description></item>
		/// </list>
		/// </param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of the ended poll</returns>
		/// <exception cref="Exception">Gets thrown when the user isn't logged in.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#end-poll">Check out the Twitch API Reference docs.</a></remarks>
		public Task<ResponseBase<PollData>?> EndPoll(string pollId, PollStatus pollStatus, CancellationToken? cancellationToken = null)
		{
			var loggedInUser = _twitchAuthService.LoggedInUser;
			if (loggedInUser == null)
			{
				throw new Exception("The user wasn't logged in yet. Try again later.");
			}

			var userId = loggedInUser.Value.UserId;

			if (string.IsNullOrWhiteSpace(pollId))
			{
				throw new ArgumentException("The pollId parameter should not be null, empty or whitespace.", nameof(pollId));
			}

			if (pollStatus is not (PollStatus.Archived or PollStatus.Terminated))
			{
				throw new ArgumentException("The pollStatus parameter may only be set to Archived or Terminated.", nameof(pollStatus));
			}

			var body = new EndPollRequestDto(userId, pollId, pollStatus);
			return PatchAsync<ResponseBase<PollData>, EndPollRequestDto>($"{TWITCH_HELIX_BASEURL}polls", body, cancellationToken);
		}

		// ReSharper disable once CognitiveComplexity
		public Task<ResponseBaseWithPagination<PredictionData>?> GetPredictions(List<string>? predictionIds = null, uint? limit = null, string? continuationCursor = null, CancellationToken? cancellationToken = null)
		{
			var loggedInUser = _twitchAuthService.LoggedInUser;
			if (loggedInUser == null)
			{
				throw new Exception("The user wasn't logged in yet. Try again later.");
			}

			var urlBuilder = new StringBuilder($"{TWITCH_HELIX_BASEURL}predictions?broadcaster_id={loggedInUser.Value.UserId}");
			if (predictionIds != null && predictionIds.Any())
			{
				if (predictionIds.Count > 100)
				{
					throw new ArgumentException("The predictionIds parameter has an upper-limit of 100.", nameof(predictionIds));
				}

				foreach (var predictionId in predictionIds)
				{
					urlBuilder.Append($"&id={predictionId}");
				}
			}

			if (limit != null)
			{
				if (limit.Value > 20)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 20.", nameof(limit));
				}

				urlBuilder.Append($"&first={limit}");
			}

			if (continuationCursor != null)
			{
				if (string.IsNullOrWhiteSpace(continuationCursor))
				{
					throw new ArgumentException("The continuationCursor parameter should not be null, empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append($"&after={continuationCursor}");
			}

			return GetAsync<ResponseBaseWithPagination<PredictionData>>(urlBuilder.ToString(), cancellationToken);
		}

		public Task<ResponseBase<PredictionData>?> CreatePrediction(string title, List<string> outcomes, int duration, CancellationToken? cancellationToken = null)
		{
			var loggedInUser = _twitchAuthService.LoggedInUser;
			if (loggedInUser == null)
			{
				throw new Exception("The user wasn't logged in yet. Try again later.");
			}

			var userId = loggedInUser.Value.UserId;

			if (string.IsNullOrWhiteSpace(title) || title.Length > 45)
			{
				throw new ArgumentException("The title argument is enforced to be 45 characters tops by Helix. Please use a shorter one.", nameof(title));
			}

			if (outcomes.Count != 2)
			{
				throw new ArgumentException("The outcomes argument is enforced to 2 entries by Helix. Please ensure that 2 outcomes are provided", nameof(outcomes));
			}

			var predictionOutcomes = new List<Outcome>();
			foreach (var outcome in outcomes)
			{
				if (string.IsNullOrWhiteSpace(outcome) || outcome.Length > 25)
				{
					throw new ArgumentException(
						"The outcomes argument is enforced to have all its entries be 25 characters tops by Helix. Please make sure that all entries are 25 characters long or shorter.",
						nameof(outcomes));
				}

				predictionOutcomes.Add(new Outcome(outcome));
			}

			if (duration is < 15 or > 1800)
			{
				throw new ArgumentException(
					"The duration argument is enforced to last at least 15 seconds and 1800 seconds tops by Helix. Please make sure that all entries are 25 characters long or shorter.",
					nameof(duration));
			}

			var body = new CreatePredictionsRequestDto(userId, title, predictionOutcomes, duration);
			return PostAsync<ResponseBase<PredictionData>, CreatePredictionsRequestDto>($"{TWITCH_HELIX_BASEURL}predictions", body, cancellationToken);
		}

		public Task<ResponseBase<PredictionData>?> EndPrediction(string predictionId, PredictionStatus predictionStatus, string? winningOutcomeId = null, CancellationToken? cancellationToken = null)
		{
			var loggedInUser = _twitchAuthService.LoggedInUser;
			if (loggedInUser == null)
			{
				throw new Exception("The user wasn't logged in yet. Try again later.");
			}

			var userId = loggedInUser.Value.UserId;

			if (string.IsNullOrWhiteSpace(predictionId))
			{
				throw new ArgumentException("The predictionId parameter should not be null, empty or whitespace.", nameof(predictionId));
			}

			if (predictionStatus is not (PredictionStatus.Cancelled or PredictionStatus.Locked or PredictionStatus.Resolved))
			{
				throw new ArgumentException("The predictionStatus parameter may only be set to Cancelled, Locked or Resolved.", nameof(predictionStatus));
			}

			if (predictionStatus == PredictionStatus.Resolved && string.IsNullOrWhiteSpace(winningOutcomeId))
			{
				throw new ArgumentException("The winningOutcomeId parameter is required when the predictionStatus parameter is set to Resolved.", nameof(winningOutcomeId));
			}

			var body = new EndPredictionRequestDto(userId, predictionId, predictionStatus, winningOutcomeId);
			return PatchAsync<ResponseBase<PredictionData>, EndPredictionRequestDto>($"{TWITCH_HELIX_BASEURL}predictions", body, cancellationToken);
		}
	}
}