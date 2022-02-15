using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Models.Twitch.Helix.Responses.Badges;
using CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes;
using CatCore.Models.Twitch.Helix.Responses.Emotes;
using CatCore.Models.Twitch.Helix.Responses.Polls;
using CatCore.Models.Twitch.Helix.Responses.Predictions;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchHelixApiService
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
		Task<ResponseBase<UserData>?> FetchUserInfo(string[]? userIds = null, string[]? loginNames = null, CancellationToken cancellationToken = default);

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
		Task<ResponseBase<CreateStreamMarkerData>?> CreateStreamMarker(string userId, string? description = null, CancellationToken cancellationToken = default);

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
		Task<ResponseBaseWithPagination<ChannelData>?> SearchChannels(string query, uint? limit = null, bool? liveOnly = null, string? continuationCursor = null,
			CancellationToken cancellationToken = default);

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
		Task<ResponseBaseWithPagination<PollData>?> GetPolls(List<string>? pollIds = null, uint? limit = null, string? continuationCursor = null, CancellationToken cancellationToken = default);

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
		Task<ResponseBase<PollData>?> CreatePoll(string title, List<string> choices, uint duration, bool? bitsVotingEnabled = null, uint? bitsPerVote = null,
			bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null, CancellationToken cancellationToken = default);

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
		Task<ResponseBase<PollData>?> EndPoll(string pollId, PollStatus pollStatus, CancellationToken cancellationToken = default);

		/// <summary>
		/// Get information about all Channel Points Predictions or specific Channel Points Predictions for a Twitch channel.
		/// Results are ordered by most recent, so it can be assumed that the currently active or locked Prediction will be the first item.
		/// </summary>
		/// <param name="predictionIds">Filters results to one or more specific predictions. Not providing one or more IDs will return the full list of polls for the authenticated channel. Maximum: 100</param>
		/// <param name="limit">Maximum number of results to return. Maximum: 20 Default: 20</param>
		/// <param name="continuationCursor">Cursor for forward pagination: tells the server where to start fetching the next set of results, in a multi-page response</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of 0, 1 or more more predictions</returns>
		/// <exception cref="Exception">Gets thrown when the user isn't logged in.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-predictions">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBaseWithPagination<PredictionData>?> GetPredictions(List<string>? predictionIds = null, uint? limit = null, string? continuationCursor = null,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Create a Channel Points Prediction for a specific Twitch channel.
		/// </summary>
		/// <param name="title">Title for the Prediction. May not exceed a length of 45 characters.</param>
		/// <param name="outcomes">
		/// Array of outcome objects with titles for the Prediction. Array size must be 2.
		/// The first outcome object is the “blue” outcome and the second outcome object is the “pink” outcome when viewing the Prediction on Twitch.
		/// Outcome entries may not exceed a length of 25 characters.
		/// </param>
		/// <param name="duration">Total duration for the Prediction (in seconds). Minimum: 1. Maximum: 1800.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of the newly created prediction</returns>
		/// <exception cref="Exception">Gets thrown when the user isn't logged in.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#create-prediction">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<PredictionData>?> CreatePrediction(string title, List<string> outcomes, uint duration, CancellationToken cancellationToken = default);

		/// <summary>
		/// Lock, resolve, or cancel a Channel Points Prediction. Active Predictions can be updated to be <see cref="PredictionStatus.Locked"/>, <see cref="PredictionStatus.Resolved"/>,
		/// or <see cref="PredictionStatus.Cancelled"/>. Locked Predictions can be updated to be <see cref="PredictionStatus.Resolved"/> or <see cref="PredictionStatus.Cancelled"/>.
		/// </summary>
		/// <param name="predictionId">Id of the Prediction.</param>
		/// <param name="predictionStatus">The Prediction status to be set. Valid values:
		/// <list type="bullet">
		/// <item><description><see cref="PredictionStatus.Resolved"/>: A winning outcome has been chosen and the Channel Points have been distributed to the users who predicted the correct outcome.</description></item>
		/// <item><description><see cref="PredictionStatus.Cancelled"/>: The Prediction has been canceled and the Channel Points have been refunded to participants.</description></item>
		/// <item><description><see cref="PredictionStatus.Locked"/>: The Prediction has been locked and viewers can no longer make predictions.</description></item>
		/// </list>
		/// </param>
		/// <param name="winningOutcomeId">Id of the winning outcome for the Prediction. This parameter is required if <paramref name="predictionStatus" /> is being set to <see cref="PredictionStatus.Resolved"/>.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of the ended prediction</returns>
		/// <exception cref="Exception">Gets thrown when the user isn't logged in.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#end-prediction">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<PredictionData>?> EndPrediction(string predictionId, PredictionStatus predictionStatus, string? winningOutcomeId = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the list of available Cheermotes, animated emotes to which viewers can assign Bits, to cheer in chat.
		/// Cheermotes returned are available throughout Twitch, in all Bits-enabled channels.
		/// </summary>
		/// <param name="userId">Id of the channel for which to retrieve Cheermotes. When no userId is passed or null, it will return all globally available Cheermotes.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of cheermotes</returns>
		/// <exception cref="Exception">Gets thrown when the user isn't logged in.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-cheermotes">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<CheermoteGroupData>?> GetCheermotes(string? userId = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets a list of chat badges that can be used in chat for any channel.
		/// </summary>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of globally available custom chat badges</returns>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-global-chat-badges">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<BadgeData>?> GetGlobalBadges(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets a list of custom chat badges that can be used in chat for the specified channel. This includes <a href="https://help.twitch.tv/s/article/subscriber-badge-guide">subscriber badges</a>
		/// and <a href="https://help.twitch.tv/s/article/custom-bit-badges-guide">Bit badges</a>.
		/// </summary>
		/// <param name="userId">Id of the channel for which to retrieve the custom chat badges.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of custom chat badges</returns>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-channel-chat-badges">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<BadgeData>?> GetBadgesForChannel(string userId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets information about active streams belonging to channels that the authenticated user follows. Streams are returned sorted by number of current viewers, in descending order.
		/// Across multiple pages of results, there may be duplicate or missing streams, as viewers join and leave streams.
		/// </summary>
		/// <param name="limit">Maximum number of results to return. Maximum: 100 Default: 100</param>
		/// <param name="continuationCursor">Cursor for forward pagination: tells the server where to start fetching the next set of results, in a multi-page response</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of 0, 1 or more more followed streams</returns>
		/// <exception cref="Exception">Gets thrown when the user isn't logged in.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-followed-streams">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBaseWithPagination<Stream>?> GetFollowedStreams(uint? limit = null, string? continuationCursor = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets information about active streams. Streams are returned sorted by number of current viewers, in descending order.
		/// Across multiple pages of results, there may be duplicate or missing streams, as viewers join and leave streams.
		/// </summary>
		/// <param name="userIds">Returns streams broadcast by one or more specified user IDs. You can specify up to 100 IDs.</param>
		/// <param name="loginNames">Returns streams broadcast by one or more specified user login names. You can specify up to 100 names.</param>
		/// <param name="gameIds">Returns streams broadcasting a specified game ID. You can specify up to 100 IDs.</param>
		/// <param name="languages">
		/// Stream language. You can specify up to 100 languages.
		/// A language value must be either the <a href="https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes">ISO 639-1</a> two-letter code for a
		/// <a href="https://help.twitch.tv/s/article/languages-on-twitch#streamlang">supported stream language</a> or “other”.
		/// </param>
		/// <param name="limit">Maximum number of results to return. Maximum: 100 Default: 20</param>
		/// <param name="continuationCursorBefore">Cursor for backward pagination: tells the server where to start fetching the next set of results, in a multi-page response</param>
		/// <param name="continuationCursorAfter">Cursor for forward pagination: tells the server where to start fetching the next set of results, in a multi-page response</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of 0, 1 or more more active streams</returns>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-streams">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBaseWithPagination<Stream>?> GetStreams(string[]? userIds = null, string[]? loginNames = null, string[]? gameIds = null, string[]? languages = null, uint? limit = null,
			string? continuationCursorBefore = null, string? continuationCursorAfter = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all <a href="https://www.twitch.tv/creatorcamp/en/learn-the-basics/emotes/">global emotes</a>. Global emotes are Twitch-created emoticons that users can use in any Twitch chat.
		/// </summary>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of globally available chat emotes</returns>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-global-emotes">Check out the Twitch API Reference docs.</a></remarks>
		public Task<ResponseBaseWithTemplate<GlobalEmote>?> GetGlobalEmotes(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all emotes that the specified Twitch channel created. Broadcasters create these custom emotes for users who subscribe to or follow the channel, or cheer Bits in the channel’s chat
		/// window. For information about the custom emotes, see <a href="https://help.twitch.tv/s/article/subscriber-emote-guide">subscriber emotes</a>,
		/// <a href="https://help.twitch.tv/s/article/custom-bit-badges-guide?language=bg#slots">Bits tier emotes</a>, and
		/// <a href="https://blog.twitch.tv/en/2021/06/04/kicking-off-10-years-with-our-biggest-emote-update-ever/">follower emotes</a>.
		/// <b>NOTE:</b> With the exception of custom follower emotes, users may use custom emotes in any Twitch chat.
		/// </summary>
		/// <param name="userId">Id of the channel for which to retrieve the custom chat emotes.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of custom chat emotes</returns>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-channel-chat-badges">Check out the Twitch API Reference docs.</a></remarks>
		public Task<ResponseBaseWithTemplate<ChannelEmote>?> GetChannelEmotes(string userId, CancellationToken cancellationToken = default);
	}
}