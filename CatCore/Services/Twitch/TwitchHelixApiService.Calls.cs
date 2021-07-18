using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Models.Twitch.Helix.Requests;
using CatCore.Models.Twitch.Helix.Requests.Polls;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Models.Twitch.Helix.Responses.Polls;
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

			return GetAsyncS<ResponseBase<UserData>>(uriBuilder.ToString(), cancellationToken);
		}

		public Task<ResponseBase<CreateStreamMarkerData>?> CreateStreamMarker(string userId, string? description = null, CancellationToken? cancellationToken = null)
		{
			if (!string.IsNullOrWhiteSpace(description) && description!.Length > 140)
			{
				throw new ArgumentException("The description argument is enforced to be 140 characters tops by Helix. Please use a shorter one.", nameof(description));
			}

			var body = new CreateStreamMarkerRequestDto(userId, description);
			return PostAsyncS<ResponseBase<CreateStreamMarkerData>, CreateStreamMarkerRequestDto>($"{TWITCH_HELIX_BASEURL}streams/markers", body, cancellationToken);
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
				if (string.IsNullOrWhiteSpace(query))
				{
					throw new ArgumentException("The continuationCursor parameter should not be null, empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append($"&after={continuationCursor}");
			}

			return GetAsyncS<ResponseBaseWithPagination<ChannelData>>(urlBuilder.ToString(), cancellationToken);
		}

		// ReSharper disable once CognitiveComplexity
		public Task<ResponseBase<PollData>?> CreatePoll(string title, List<string> choices, int duration, bool? bitsVotingEnabled = null, uint? bitsPerVote = null,
			bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null, CancellationToken? cancellationToken = null)
		{
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
							costPerVote = 0;
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

			var body = new CreatePollRequestDto(_twitchAuthService.LoggedInUser.GetValueOrDefault().UserId, title, pollChoices, duration, bitsVotingEnabled, bitsPerVote, channelPointsVotingEnabled,
				channelPointsPerVote);
			return PostAsyncS<ResponseBase<PollData>, CreatePollRequestDto>($"{TWITCH_HELIX_BASEURL}polls", body, cancellationToken);
		}
	}
}