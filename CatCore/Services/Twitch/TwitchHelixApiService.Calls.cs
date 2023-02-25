using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers.JSON;
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
using CatCore.Models.Twitch.Shared;
using CatCore.Services.Twitch.Interfaces;
using Outcome = CatCore.Models.Twitch.Helix.Requests.Predictions.Outcome;
using PollChoice = CatCore.Models.Twitch.Helix.Requests.Polls.PollChoice;

namespace CatCore.Services.Twitch
{
	public sealed partial class TwitchHelixApiService : ITwitchHelixApiService
	{
		/// <inheritdoc />
		public async Task<ResponseBase<UserData>?> FetchUserInfo(string[]? userIds = null, string[]? loginNames = null, CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "users");

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
				urlBuilder.Append("?");
			}

			if (hasUserIds)
			{
				urlBuilder.Append("id=").Append(string.Join("&id=", userIds!));
			}

			if (hasLoginNames)
			{
				urlBuilder.Append("login=").Append(string.Join("&login=", loginNames!));
			}

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseUserData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<CreateStreamMarkerData>?> CreateStreamMarker(string userId, string? description = null, CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);

			if (!string.IsNullOrWhiteSpace(description) && description!.Length > 140)
			{
				throw new ArgumentException("The description argument is enforced to be 140 characters tops by Helix. Please use a shorter one.", nameof(description));
			}

			var body = new CreateStreamMarkerRequestDto(userId, description);
			return await PostAsync(TWITCH_HELIX_BASEURL + "streams/markers", body, TwitchHelixSerializerContext.Default.ResponseBaseCreateStreamMarkerData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBaseWithPagination<ChannelData>?> SearchChannels(string query, uint? limit = null, bool? liveOnly = null, string? continuationCursor = null,
			CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);

			if (string.IsNullOrWhiteSpace(query))
			{
				throw new ArgumentException("The query parameter should not be null, empty or whitespace.", nameof(query));
			}

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "search/channels?query=" + query);
			if (limit != null)
			{
				if (limit.Value > 100)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 100.", nameof(limit));
				}

				urlBuilder.Append("&first=").Append(limit);
			}

			if (liveOnly != null)
			{
				urlBuilder.Append("&live_only=").Append(liveOnly);
			}

			if (continuationCursor != null)
			{
				if (string.IsNullOrWhiteSpace(continuationCursor))
				{
					throw new ArgumentException("The continuationCursor parameter should not be null, empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append("&after=").Append(continuationCursor);
			}

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseWithPaginationChannelData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<ChatSettings>?> GetChatSettings(string broadcasterId, bool withModeratorPermissions = false, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "chat/settings?broadcaster_id=" + broadcasterId);
			if (withModeratorPermissions)
			{
				urlBuilder.Append("&moderator_id=").Append(loggedInUser.UserId);
			}

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseChatSettings, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		// ReSharper disable once CognitiveComplexity
		public async Task<ResponseBase<ChatSettings>?> UpdateChatSettings(string broadcasterId, bool? emoteMode = null, bool? followerMode = null, uint? followerModeDurationMinutes = null,
			bool? nonModeratorChatDelay = null, uint? nonModeratorChatDelayDurationSeconds = null, bool? slowMode = null, uint? slowModeWaitTimeSeconds = null, bool? subscriberMode = null,
			bool? uniqueChatMode = null, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = TWITCH_HELIX_BASEURL + "chat/settings?broadcaster_id=" + broadcasterId + "&moderator_id=" + loggedInUser.UserId;

			if (followerModeDurationMinutes != null)
			{
				followerMode = true;

				if (followerModeDurationMinutes.Value > 129600)
				{
					throw new ArgumentException("The followerModeDurationMinutes parameter should be less than or equal to 129600 (3 months).", nameof(followerModeDurationMinutes));
				}
			}

			if (nonModeratorChatDelayDurationSeconds != null)
			{
				nonModeratorChatDelay = true;

				if (nonModeratorChatDelayDurationSeconds is not 2 and not 4 and not 6)
				{
					throw new ArgumentException("The nonModeratorChatDelayDurationSeconds parameter should be 2, 4 or 6.", nameof(nonModeratorChatDelayDurationSeconds));
				}
			}

			if (slowModeWaitTimeSeconds != null)
			{
				slowMode = true;

				switch (slowModeWaitTimeSeconds.Value)
				{
					case < 3:
						throw new ArgumentException("The slowModeWaitTimeSeconds parameter should be greater than or equal to 3.", nameof(slowModeWaitTimeSeconds));
					case > 120:
						throw new ArgumentException("The slowModeWaitTimeSeconds parameter should be less than or equal to 120.", nameof(slowModeWaitTimeSeconds));
				}
			}

			var body = new ChatSettingsRequestDto(emoteMode, followerMode, followerModeDurationMinutes, nonModeratorChatDelay, nonModeratorChatDelayDurationSeconds, slowMode,
				slowModeWaitTimeSeconds, subscriberMode, uniqueChatMode);

			return await PatchAsync(urlBuilder, body, TwitchHelixSerializerContext.Default.ResponseBaseChatSettings, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBaseWithPagination<BannedUserInfo>?> GetBannedUsers(string[]? userIds = null, uint? limit = null, string? continuationCursorBefore = null,
			string? continuationCursorAfter = null, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "moderation/banned?broadcaster_id=" + loggedInUser.UserId);

			if (limit != null)
			{
				if (limit.Value > 100)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 100.", nameof(limit));
				}

				urlBuilder.Append($"first={limit}");
			}

			if (userIds != null)
			{
				if (userIds.Length > 100)
				{
					throw new ArgumentException("The userIds parameter has an upper-limit of 100.", nameof(userIds));
				}

				urlBuilder.Append("user_id=").Append(string.Join("&user_id=", userIds));
			}

			if (continuationCursorBefore != null && continuationCursorAfter != null)
			{
				throw new ArgumentException("The continuationCursorBefore and continuationCursorAfter cannot be specified both simultaneously",
					$"{nameof(continuationCursorBefore)} | {nameof(continuationCursorAfter)}");
			}

			if (!string.IsNullOrWhiteSpace(continuationCursorBefore))
			{
				urlBuilder.Append(string.Join("before=", continuationCursorBefore));
			}
			else if (!string.IsNullOrWhiteSpace(continuationCursorAfter))
			{
				urlBuilder.Append(string.Join("after=", continuationCursorAfter));
			}

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseWithPaginationBannedUserInfo, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<BanUser>?> BanUser(string broadcasterId, string userId, uint? durationSeconds, string? reason = null, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = TWITCH_HELIX_BASEURL + "moderation/bans?broadcaster_id=" + broadcasterId + "&moderator_id=" + loggedInUser.UserId;

			if (durationSeconds != null)
			{
				if (durationSeconds < 1)
				{
					throw new ArgumentException("The durationSeconds parameter should be greater than or equal to 1. If you want to ban the user, use null instead.", nameof(durationSeconds));
				}

				if (durationSeconds > 1209600)
				{
					throw new ArgumentException("The durationSeconds parameter should be less than or equal to 1209600 (2 weeks).", nameof(durationSeconds));
				}
			}

			var body = new BanUserRequestDto(userId, durationSeconds, reason);
			var bodyWrapper = new LegacyRequestDataWrapper<BanUserRequestDto>(body);
			return await PostAsync(urlBuilder, bodyWrapper, TwitchHelixSerializerContext.Default.ResponseBaseBanUser, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool> UnbanUser(string broadcasterId, string userId, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = TWITCH_HELIX_BASEURL + "moderation/bans?broadcaster_id=" + broadcasterId + "&moderator_id=" + loggedInUser.UserId + "&user_id=" + userId;

			return await DeleteAsync(urlBuilder, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool> SendChatAnnouncement(string broadcasterId, string message, SendChatAnnouncementColor color = SendChatAnnouncementColor.Primary, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = TWITCH_HELIX_BASEURL + "chat/announcements?broadcaster_id=" + broadcasterId + "&moderator_id=" + loggedInUser.UserId;

			var body = new SendChatAnnouncementRequestDto(message, color);
			return await PostAsync(urlBuilder, body, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool> DeleteChatMessages(string broadcasterId, string? messageId = null, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "moderation/chat?broadcaster_id=" + broadcasterId + "&moderator_id=" + loggedInUser.UserId);
			if (messageId != null)
			{
				if (string.IsNullOrWhiteSpace(messageId))
				{
					throw new ArgumentException("The messageId parameter should not be empty.", nameof(messageId));
				}

				urlBuilder.Append("&message_id=").Append(messageId);
			}

			return await DeleteAsync(urlBuilder.ToString(), cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<UserChatColorData>?> GetUserChatColor(string[] userIds, CancellationToken cancellationToken = default)
		{
			_ = await CheckUserLoggedIn().ConfigureAwait(false);

			if (userIds.Length > 100)
			{
				throw new ArgumentException("The userIds parameter has an upper-limit of 100.", nameof(userIds));
			}

			if (userIds.Length == 0)
			{
				throw new ArgumentException("The userIds parameter should not be empty.", nameof(userIds));
			}

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "chat/color?user_id=").Append(string.Join("&user_id=", userIds));

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseUserChatColorData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool> UpdateUserChatColor(UserChatColor color, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var colorString = color switch
			{
				UserChatColor.Blue => "blue",
				UserChatColor.BlueViolet => "blue_violet",
				UserChatColor.CadetBlue => "cadet_blue",
				UserChatColor.Chocolate => "chocolate",
				UserChatColor.Coral => "coral",
				UserChatColor.DodgerBlue => "dodger_blue",
				UserChatColor.Firebrick => "firebrick",
				UserChatColor.GoldenRod => "golden_rod",
				UserChatColor.Green => "green",
				UserChatColor.HotPink => "hot_pink",
				UserChatColor.OrangeRed => "orange_red",
				UserChatColor.Red => "red",
				UserChatColor.SeaGreen => "sea_green",
				UserChatColor.SpringGreen => "spring_green",
				UserChatColor.YellowGreen => "yellow_green",
				_ => throw new ArgumentOutOfRangeException(nameof(color), color, "An invalid color was provided.")
			};

			var url = TWITCH_HELIX_BASEURL + "chat/color?user_id=" + loggedInUser.UserId + "&color=" + colorString;
			return await PutAsync(url, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<StartRaidData>?> StartRaid(string targetBroadcasterId, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var url = TWITCH_HELIX_BASEURL + "raids?from_broadcaster_id=" + loggedInUser.UserId + "&to_broadcaster_id=" + targetBroadcasterId;
			return await PostAsync(url, TwitchHelixSerializerContext.Default.ResponseBaseStartRaidData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool> CancelRaid(CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			return await DeleteAsync(TWITCH_HELIX_BASEURL + "raids?broadcaster_id=" + loggedInUser.UserId, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		// ReSharper disable once CognitiveComplexity
		public async Task<ResponseBaseWithPagination<PollData>?> GetPolls(List<string>? pollIds = null, uint? limit = null, string? continuationCursor = null, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "polls?broadcaster_id=" + loggedInUser.UserId);
			if (pollIds != null && pollIds.Any())
			{
				if (pollIds.Count > 100)
				{
					throw new ArgumentException("The pollIds parameter has an upper-limit of 100.", nameof(pollIds));
				}

				foreach (var pollId in pollIds)
				{
					urlBuilder.Append("&id=").Append(pollId);
				}
			}

			if (limit != null)
			{
				if (limit.Value > 20)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 20.", nameof(limit));
				}

				urlBuilder.Append("&first=").Append(limit);
			}

			if (continuationCursor != null)
			{
				if (string.IsNullOrWhiteSpace(continuationCursor))
				{
					throw new ArgumentException("The continuationCursor parameter should not be null, empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append("&after=").Append(continuationCursor);
			}

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseWithPaginationPollData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		[Obsolete("This method is deprecated, please use the CreatePoll(string title, List<string> choices, uint duration, bool? bitsVotingEnabled = null, uint? bitsPerVote = null, bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null, CancellationToken cancellationToken = default) method instead.", true)]
		public Task<ResponseBase<PollData>?> CreatePoll(string title, List<string> choices, uint duration, bool? bitsVotingEnabled = null, uint? bitsPerVote = null,
			bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null, CancellationToken cancellationToken = default)
			=> CreatePoll(title, choices, duration, channelPointsVotingEnabled, channelPointsPerVote, cancellationToken);

		/// <inheritdoc />
		// ReSharper disable once CognitiveComplexity
		public async Task<ResponseBase<PollData>?> CreatePoll(string title, List<string> choices, uint duration, bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null,
			CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			if (string.IsNullOrWhiteSpace(title) || title.Length > 60)
			{
				throw new ArgumentException("The title argument is enforced to be 60 characters tops by Helix. Please use a shorter one.", nameof(title));
			}

			if (choices.Count is < 2 or > 5)
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

			OptionalParametersValidation(ref channelPointsVotingEnabled, ref channelPointsPerVote, 1000000);

			var body = new CreatePollRequestDto(loggedInUser.UserId, title, pollChoices, duration, channelPointsVotingEnabled, channelPointsPerVote);
			return await PostAsync(TWITCH_HELIX_BASEURL + "polls", body, TwitchHelixSerializerContext.Default.ResponseBasePollData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<PollData>?> EndPoll(string pollId, PollStatus pollStatus, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			if (string.IsNullOrWhiteSpace(pollId))
			{
				throw new ArgumentException("The pollId parameter should not be null, empty or whitespace.", nameof(pollId));
			}

			if (pollStatus is not (PollStatus.Archived or PollStatus.Terminated))
			{
				throw new ArgumentException("The pollStatus parameter may only be set to Archived or Terminated.", nameof(pollStatus));
			}

			var body = new EndPollRequestDto(loggedInUser.UserId, pollId, pollStatus);
			return await PatchAsync(TWITCH_HELIX_BASEURL + "polls", body, TwitchHelixSerializerContext.Default.ResponseBasePollData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		// ReSharper disable once CognitiveComplexity
		public async Task<ResponseBaseWithPagination<PredictionData>?> GetPredictions(List<string>? predictionIds = null, uint? limit = null, string? continuationCursor = null,
			CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "predictions?broadcaster_id=" + loggedInUser.UserId);
			if (predictionIds != null && predictionIds.Any())
			{
				if (predictionIds.Count > 100)
				{
					throw new ArgumentException("The predictionIds parameter has an upper-limit of 100.", nameof(predictionIds));
				}

				foreach (var predictionId in predictionIds)
				{
					urlBuilder.Append("&id=").Append(predictionId);
				}
			}

			if (limit != null)
			{
				if (limit.Value > 20)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 20.", nameof(limit));
				}

				urlBuilder.Append("&first=").Append(limit);
			}

			if (continuationCursor != null)
			{
				if (string.IsNullOrWhiteSpace(continuationCursor))
				{
					throw new ArgumentException("The continuationCursor parameter should not be empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append("&after=").Append(continuationCursor);
			}

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseWithPaginationPredictionData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<PredictionData>?> CreatePrediction(string title, List<string> outcomes, uint duration, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

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

			var body = new CreatePredictionsRequestDto(loggedInUser.UserId, title, predictionOutcomes, duration);
			return await PostAsync(TWITCH_HELIX_BASEURL + "predictions", body, TwitchHelixSerializerContext.Default.ResponseBasePredictionData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<PredictionData>?> EndPrediction(string predictionId, PredictionStatus predictionStatus, string? winningOutcomeId = null, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

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

			var body = new EndPredictionRequestDto(loggedInUser.UserId, predictionId, predictionStatus, winningOutcomeId);
			return await PatchAsync(TWITCH_HELIX_BASEURL + "predictions", body, TwitchHelixSerializerContext.Default.ResponseBasePredictionData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<CheermoteGroupData>?> GetCheermotes(string? userId = null, CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "bits/cheermotes");
			if (!string.IsNullOrWhiteSpace(userId))
			{
				urlBuilder.Append("?broadcaster_id=").Append(userId);
			}

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseCheermoteGroupData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<BadgeData>?> GetGlobalBadges(CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);
			return await GetAsync(TWITCH_HELIX_BASEURL + "chat/badges/global", TwitchHelixSerializerContext.Default.ResponseBaseBadgeData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBase<BadgeData>?> GetBadgesForChannel(string userId, CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);

			if (string.IsNullOrWhiteSpace(userId))
			{
				throw new ArgumentException("The userId parameter should not be null, empty or whitespace.", nameof(userId));
			}

			return await GetAsync(TWITCH_HELIX_BASEURL + "chat/badges?broadcaster_id=" + userId, TwitchHelixSerializerContext.Default.ResponseBaseBadgeData, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBaseWithPagination<Stream>?> GetFollowedStreams(uint? limit = null, string? continuationCursor = null, CancellationToken cancellationToken = default)
		{
			var loggedInUser = await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "streams/followed?user_id=" + loggedInUser.UserId);

			if (limit != null)
			{
				if (limit.Value > 100)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 100.", nameof(limit));
				}

				urlBuilder.Append($"&first={limit}");
			}

			if (continuationCursor != null)
			{
				if (string.IsNullOrWhiteSpace(continuationCursor))
				{
					throw new ArgumentException("The continuationCursor parameter should not be empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append($"&after={continuationCursor}");
			}

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseWithPaginationStream, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		// ReSharper disable once CognitiveComplexity
		public async Task<ResponseBaseWithPagination<Stream>?> GetStreams(string[]? userIds = null, string[]? loginNames = null, string[]? gameIds = null, string[]? languages = null,
			uint? limit = null, string? continuationCursorBefore = null, string? continuationCursorAfter = null, CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);

			var urlBuilder = new StringBuilder(TWITCH_HELIX_BASEURL + "streams");

			var firstQueryParamSet = false;

			char QueryParamSeparator()
			{
				if (firstQueryParamSet)
				{
					return '&';
				}

				firstQueryParamSet = true;
				return '?';
			}

			if (limit != null)
			{
				if (limit.Value > 100)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 100.", nameof(limit));
				}

				urlBuilder.Append(QueryParamSeparator()).Append($"first={limit}");
			}

			if (userIds != null)
			{
				if (userIds.Length > 100)
				{
					throw new ArgumentException("The userIds parameter has an upper-limit of 100.", nameof(userIds));
				}

				urlBuilder.Append(QueryParamSeparator()).Append("user_id=").Append(string.Join("&user_id=", userIds));
			}

			if (loginNames != null)
			{
				if (loginNames.Length > 100)
				{
					throw new ArgumentException("The loginNames parameter has an upper-limit of 100.", nameof(loginNames));
				}

				urlBuilder.Append(QueryParamSeparator()).Append("user_login=").Append(string.Join("&user_login=", loginNames));
			}

			if (gameIds != null)
			{
				if (gameIds.Length > 100)
				{
					throw new ArgumentException("The gameIds parameter has an upper-limit of 100.", nameof(gameIds));
				}

				urlBuilder.Append(QueryParamSeparator()).Append("game_id=").Append(string.Join("&game_id=", gameIds));
			}

			if (languages != null)
			{
				if (languages.Length > 100)
				{
					throw new ArgumentException("The languages parameter has an upper-limit of 100.", nameof(languages));
				}

				urlBuilder.Append(QueryParamSeparator()).Append("language=").Append(string.Join("&language=", languages));
			}

			if (continuationCursorBefore != null && continuationCursorAfter != null)
			{
				throw new ArgumentException("The continuationCursorBefore and continuationCursorAfter cannot be specified both simultaneously",
					$"{nameof(continuationCursorBefore)} | {nameof(continuationCursorAfter)}");
			}

			if (!string.IsNullOrWhiteSpace(continuationCursorBefore))
			{
				urlBuilder.Append(QueryParamSeparator()).Append(string.Join("before=", continuationCursorBefore));
			}
			else if (!string.IsNullOrWhiteSpace(continuationCursorAfter))
			{
				urlBuilder.Append(QueryParamSeparator()).Append(string.Join("after=", continuationCursorAfter));
			}

			return await GetAsync(urlBuilder.ToString(), TwitchHelixSerializerContext.Default.ResponseBaseWithPaginationStream, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBaseWithTemplate<GlobalEmote>?> GetGlobalEmotes(CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);
			return await GetAsync(TWITCH_HELIX_BASEURL + "chat/emotes/global", TwitchHelixSerializerContext.Default.ResponseBaseWithTemplateGlobalEmote, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ResponseBaseWithTemplate<ChannelEmote>?> GetChannelEmotes(string userId, CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);

			if (string.IsNullOrWhiteSpace(userId))
			{
				throw new ArgumentException("The userId parameter should not be null, empty or whitespace.", nameof(userId));
			}

			return await GetAsync(TWITCH_HELIX_BASEURL + "chat/emotes?broadcaster_id=" + userId, TwitchHelixSerializerContext.Default.ResponseBaseWithTemplateChannelEmote, cancellationToken).ConfigureAwait(false);
		}
	}
}