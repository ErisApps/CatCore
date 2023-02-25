using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Exceptions;
using CatCore.Models.Twitch.Helix.Requests;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Models.Twitch.Helix.Responses.Badges;
using CatCore.Models.Twitch.Helix.Responses.Bans;
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#search-channels">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBaseWithPagination<ChannelData>?> SearchChannels(string query, uint? limit = null, bool? liveOnly = null, string? continuationCursor = null,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the broadcaster’s chat settings.
		/// </summary>
		/// <param name="broadcasterId">The ID of the broadcaster whose chat settings you want to get</param>
		/// <param name="withModeratorPermissions">Check the broadcaster's chat settings as a moderator, will include NonModeratorChatDelay related properties</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing the channel settings of the requested broadcaster</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#get-chat-settings">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<ChatSettings>?> GetChatSettings(string broadcasterId, bool withModeratorPermissions = false, CancellationToken cancellationToken = default);

		/// <summary>
		/// Updates the broadcaster’s chat settings.
		/// </summary>
		/// <param name="broadcasterId">The ID of the broadcaster whose chat settings you want to update</param>
		/// <param name="emoteMode">Indicates whether messages may only contain emotes or not</param>
		/// <param name="followerMode">Indicates whether the broadcaster' chat is restricted to followers only</param>
		/// <param name="followerModeDurationMinutes">The length of time, in minutes, that users must follow the broadcaster before being able to participate in the chat room. Minimum: 0 (No restrictions). Maximum: 129600 (3 months). Overrides <paramref name="followerMode" /> if set.</param>
		/// <param name="nonModeratorChatDelay">Indicates whether the broadcaster adds a short delay before chat messages appear in the chat. This gives chat moderators and bots a chance to remove them before viewers can see the message.</param>
		/// <param name="nonModeratorChatDelayDurationSeconds">The amount of time, in seconds, that messages are delayed before appearing in chat. Valid values:
		/// <list type="bullet">
		/// <item><description>2: 2 second delay (recommended)</description></item>
		/// <item><description>4: 4 second delay</description></item>
		/// <item><description>6: 6 second delay</description></item>
		/// </list>
		/// Overrides <paramref name="nonModeratorChatDelay" /> if set.
		/// </param>
		/// <param name="slowMode">Indicates that the broadcaster limits how often users are allowed to send messages in chat.</param>
		/// <param name="slowModeWaitTimeSeconds">The amount of time, in seconds, that users must wait between sending messages. Minimum: 3 (No restrictions). Maximum: 120 (2 minutes). Overrides <paramref name="slowMode" /> if set.</param>
		/// <param name="subscriberMode">Indicates whether only users who subscribe to the broadcaster's channel may send messages.</param>
		/// <param name="uniqueChatMode">Indicates whether the broadcaster requires users to post only unique messages in chat.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing the channel settings of the requested broadcaster</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#update-chat-settings">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<ChatSettings>?> UpdateChatSettings(string broadcasterId, bool? emoteMode = null, bool? followerMode = null, uint? followerModeDurationMinutes = null,
			bool? nonModeratorChatDelay = null, uint? nonModeratorChatDelayDurationSeconds = null, bool? slowMode = null, uint? slowModeWaitTimeSeconds = null, bool? subscriberMode = null,
			bool? uniqueChatMode = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all users that the broadcaster banned or put in a timeout.
		/// </summary>
		/// <param name="userIds">A list of user IDs used to filter the results. Maximum: 100</param>
		/// <param name="limit">The maximum number of items to return per page in the response. Maximum: 100. Default: 20.</param>
		/// <param name="continuationCursorBefore">The cursor used to get the previous page of results.</param>
		/// <param name="continuationCursorAfter">The cursor used to get the next page of results.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing paginated info about users who are timed-out or banned</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#get-banned-users">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBaseWithPagination<BannedUserInfo>?> GetBannedUsers(string[]? userIds = null, uint? limit = null, string? continuationCursorBefore = null,
			string? continuationCursorAfter = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Bans a user from participating in the specified broadcaster’s chat room or puts them in a timeout.
		/// If the user is currently in a timeout, you can call this endpoint to change the durationSeconds of the timeout or ban them altogether. If the user is currently banned, you cannot call this method to put them in a timeout instead.
		/// </summary>
		/// <param name="broadcasterId">The ID of the broadcaster whose chat room the user is being banned/timed-out from.</param>
		/// <param name="userId">The ID of the user to ban or put in a timeout.</param>
		/// <param name="durationSeconds">To put a user in a timeout, include this field and specify the timeout period, in seconds. Minimum: 1 second. Maximum: 1209600 seconds (2 weeks). To ban a user indefinitely, use <see langword="null" /> instead.</param>
		/// <param name="reason">he reason the you’re banning the user or putting them in a timeout. Limited to 500 characters, if the text is longer, it will be truncated.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing information about the user that just received a ban or time-out.</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#ban-user">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<BanUser>?> BanUser(string broadcasterId, string userId, uint? durationSeconds, string? reason = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Removes the ban or timeout that was placed on the specified user.
		/// </summary>
		/// <param name="broadcasterId">The ID of the broadcaster whose chat room the user is banned/timed-out from chatting in.</param>
		/// <param name="userId">The ID of the user to remove the ban or timeout from.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Boolean indicating whether the request was successful or not</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#unban-user">Check out the Twitch API Reference docs.</a></remarks>
		Task<bool> UnbanUser(string broadcasterId, string userId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Sends an announcement to the broadcaster’s chat room.
		/// </summary>
		/// <param name="broadcasterId">The ID of the broadcaster that owns the chat room to send the announcement to.</param>
		/// <param name="message">The announcement to make in the broadcaster’s chat room. Announcements are limited to a maximum of 500 characters; announcements longer than 500 characters are truncated.</param>
		/// <param name="color">The color used to highlight the announcement. Possible case-sensitive values are:
		/// <list type="bullet">
		/// <item><description><see cref="SendChatAnnouncementColor.Primary"/></description></item>
		/// <item><description><see cref="SendChatAnnouncementColor.Blue"/></description></item>
		/// <item><description><see cref="SendChatAnnouncementColor.Green"/></description></item>
		/// <item><description><see cref="SendChatAnnouncementColor.Orange"/></description></item>
		/// <item><description><see cref="SendChatAnnouncementColor.Purple"/></description></item>
		/// </list>
		/// If color is set to primary or is not set, then the channel’s accent color is used to highlight the announcement.
		/// </param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Boolean indicating whether the request was successful or not</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Gets thrown when an invalid color enum is passed in.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#send-chat-announcement">Check out the Twitch API Reference docs.</a></remarks>
		Task<bool> SendChatAnnouncement(string broadcasterId, string message, SendChatAnnouncementColor color = SendChatAnnouncementColor.Primary, CancellationToken cancellationToken = default);

		/// <summary>
		/// Removes a single chat message or all chat messages from the broadcaster’s chat room.
		/// </summary>
		/// <param name="broadcasterId"></param>
		/// <param name="messageId">The ID of the message to remove. The id tag in the PRIVMSG tag contains the message’s ID.
		/// <list type="bullet">
		/// <item><description>The message must have been created within the last 6 hours.</description></item>
		/// <item><description>The message must not belong to the broadcaster.</description></item>
		/// <item><description>The message must not belong to another moderator.</description></item>
		/// </list>
		/// If not specified, the request removes all messages in the broadcaster’s chat room.
		/// </param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Boolean indicating whether the request was successful or not</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#delete-chat-messages">Check out the Twitch API Reference docs.</a></remarks>
		Task<bool> DeleteChatMessages(string broadcasterId, string? messageId = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the color used for the user’s name in chat.
		/// </summary>
		/// <param name="userIds">The Ids of the users whose username color you want to get.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing information about the chat color of the requested users.</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#get-user-chat-color">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<UserChatColorData>?> GetUserChatColor(string[] userIds, CancellationToken cancellationToken = default);

		/// <summary>
		/// Updates the color used for the user’s name in chat.
		/// </summary>
		/// <param name="color">The color to use for the user’s name in chat. All users may specify one of the following named color values:
		/// <list type="bullet">
		/// <item><description><see cref="UserChatColor.Blue"/></description></item>
		/// <item><description><see cref="UserChatColor.BlueViolet"/></description></item>
		/// <item><description><see cref="UserChatColor.CadetBlue"/></description></item>
		/// <item><description><see cref="UserChatColor.Chocolate"/></description></item>
		/// <item><description><see cref="UserChatColor.Coral"/></description></item>
		/// <item><description><see cref="UserChatColor.DodgerBlue"/></description></item>
		/// <item><description><see cref="UserChatColor.Firebrick"/></description></item>
		/// <item><description><see cref="UserChatColor.GoldenRod"/></description></item>
		/// <item><description><see cref="UserChatColor.Green"/></description></item>
		/// <item><description><see cref="UserChatColor.HotPink"/></description></item>
		/// <item><description><see cref="UserChatColor.OrangeRed"/></description></item>
		/// <item><description><see cref="UserChatColor.Red"/></description></item>
		/// <item><description><see cref="UserChatColor.SeaGreen"/></description></item>
		/// <item><description><see cref="UserChatColor.SpringGreen"/></description></item>
		/// <item><description><see cref="UserChatColor.YellowGreen"/></description></item>
		/// </list>
		/// </param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Boolean indicating whether the request was successful or not</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Gets thrown when an invalid color enum is passed in.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#update-user-chat-color">Check out the Twitch API Reference docs.</a></remarks>
		Task<bool> UpdateUserChatColor(UserChatColor color, CancellationToken cancellationToken = default);

		/// <summary>
		/// Raid another channel by sending the broadcaster’s viewers to the targeted channel.
		/// The Twitch UX will pop up a window at the top of the chat room that identifies the number of viewers in the raid.
		/// The raid occurs when the broadcaster clicks Raid Now or after the 90-second countdown expires.
		/// </summary>
		/// <param name="targetBroadcasterId">The ID of the broadcaster to raid.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing information about the raid that got initiated.</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#start-a-raid">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<StartRaidData>?> StartRaid(string targetBroadcasterId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Cancel a pending raid.
		/// You can cancel a raid at any point up until the broadcaster clicks Raid Now in the Twitch UX or the 90-second countdown expires.
		/// </summary>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Boolean indicating whether the request was successful or not</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference/#cancel-a-raid">Check out the Twitch API Reference docs.</a></remarks>
		Task<bool> CancelRaid(CancellationToken cancellationToken = default);

		/// <summary>
		/// Get information about all polls or specific polls for a Twitch channel. Poll information is available for 90 days.
		/// </summary>
		/// <param name="pollIds">Filters results to one or more specific polls. Not providing one or more IDs will return the full list of polls for the authenticated channel. Maximum: 100</param>
		/// <param name="limit">Maximum number of results to return. Maximum: 20 Default: 20</param>
		/// <param name="continuationCursor">Cursor for forward pagination: tells the server where to start fetching the next set of results, in a multi-page response</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of 0, 1 or more more polls</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#create-poll">Check out the Twitch API Reference docs.</a></remarks>
		[Obsolete("This method is deprecated, please use the CreatePoll(string title, List<string> choices, uint duration, bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null, CancellationToken cancellationToken = default) method instead.", true)]
		Task<ResponseBase<PollData>?> CreatePoll(string title, List<string> choices, uint duration, bool? bitsVotingEnabled = null, uint? bitsPerVote = null,
			bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Create a poll for a specific Twitch channel.
		/// </summary>
		/// <param name="title">Question displayed for the poll. Maximum: 60 characters.</param>
		/// <param name="choices">Array of possible poll choices. Minimum: 2 choices. Maximum: 5 choices.</param>
		/// <param name="duration">Total duration for the poll (in seconds). Minimum: 15. Maximum: 1800.</param>
		/// <param name="channelPointsVotingEnabled">Indicates if Channel Points can be used for voting.</param>
		/// <param name="channelPointsPerVote">Number of Channel Points required to vote once with Channel Points. Minimum: 1. Maximum: 1000000.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of the newly created poll</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#create-poll">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<PollData>?> CreatePoll(string title, List<string> choices, uint duration, bool? channelPointsVotingEnabled = null, uint? channelPointsPerVote = null,
			CancellationToken cancellationToken = default);

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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-cheermotes">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<CheermoteGroupData>?> GetCheermotes(string? userId = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets a list of chat badges that can be used in chat for any channel.
		/// </summary>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of globally available custom chat badges</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-global-chat-badges">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBase<BadgeData>?> GetGlobalBadges(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets a list of custom chat badges that can be used in chat for the specified channel. This includes <a href="https://help.twitch.tv/s/article/subscriber-badge-guide">subscriber badges</a>
		/// and <a href="https://help.twitch.tv/s/article/custom-bit-badges-guide">Bit badges</a>.
		/// </summary>
		/// <param name="userId">Id of the channel for which to retrieve the custom chat badges.</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of custom chat badges</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <param name="limit">Maximum number of results to return. Maximum: 100. Default: 20.</param>
		/// <param name="continuationCursorBefore">Cursor for backward pagination: tells the server where to start fetching the next set of results, in a multi-page response</param>
		/// <param name="continuationCursorAfter">Cursor for forward pagination: tells the server where to start fetching the next set of results, in a multi-page response</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of 0, 1 or more more active streams</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <exception cref="ArgumentException">Gets thrown when validation regarding one of the arguments fails.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-streams">Check out the Twitch API Reference docs.</a></remarks>
		Task<ResponseBaseWithPagination<Stream>?> GetStreams(string[]? userIds = null, string[]? loginNames = null, string[]? gameIds = null, string[]? languages = null, uint? limit = null,
			string? continuationCursorBefore = null, string? continuationCursorAfter = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all <a href="https://www.twitch.tv/creatorcamp/en/learn-the-basics/emotes/">global emotes</a>. Global emotes are Twitch-created emoticons that users can use in any Twitch chat.
		/// </summary>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>Response containing data of globally available chat emotes</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
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
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated, either make sure the user is logged in or try again later.</exception>
		/// <remarks><a href="https://dev.twitch.tv/docs/api/reference#get-channel-chat-badges">Check out the Twitch API Reference docs.</a></remarks>
		public Task<ResponseBaseWithTemplate<ChannelEmote>?> GetChannelEmotes(string userId, CancellationToken cancellationToken = default);
	}
}