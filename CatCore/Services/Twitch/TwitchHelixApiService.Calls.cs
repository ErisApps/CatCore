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
		public Task<ResponseBase<UserData>?> FetchUserInfo(CancellationToken? cancellationToken = null, params string[] loginNames)
		{
			var uriBuilder = new StringBuilder($"{TWITCH_HELIX_BASEURL}users");
			if (loginNames.Any())
			{
				uriBuilder.Append($"?login={loginNames.First()}");
				for (var i = 1; i < loginNames.Length; i++)
				{
					var loginName = loginNames[i];
					uriBuilder.Append($"&login={loginName}");
				}
			}

			return GetAsync<ResponseBase<UserData>>(uriBuilder.ToString(), cancellationToken);
		}

		public Task<ResponseBase<CreateStreamMarkerData>?> CreateStreamMarker(string userId, string? description = null, CancellationToken? cancellationToken = null)
		{
			if (!string.IsNullOrWhiteSpace(description) && description!.Length > 140)
			{
				throw new ArgumentException("The description argument is enforced to be 140 characters tops by Helix. Please use a shorter one.", nameof(description));
			}

			var body = new CreateStreamMarkerRequestDto(userId, description);
			return PostAsync<ResponseBase<CreateStreamMarkerData>, CreateStreamMarkerRequestDto>($"{TWITCH_HELIX_BASEURL}streams/markers", body, cancellationToken);
		}

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