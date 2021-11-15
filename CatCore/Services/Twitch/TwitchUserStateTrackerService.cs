using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Twitch.Interfaces;

namespace CatCore.Services.Twitch
{
	public sealed class TwitchUserStateTrackerService : ITwitchUserStateTrackerService
	{
		private readonly ConcurrentDictionary<string, TwitchUserState> _userStates;

		internal TwitchUserStateTrackerService()
		{
			_userStates = new ConcurrentDictionary<string, TwitchUserState>();
		}

		public TwitchGlobalUserState? GlobalUserState { get; private set; }

		public TwitchUserState? GetUserState(string channelId)
		{
			return _userStates.TryGetValue(channelId, out var userState) ? userState : null;
		}

		void ITwitchUserStateTrackerService.UpdateGlobalUserState(ReadOnlyDictionary<string, string>? globalUserStateUpdate)
		{
			if (globalUserStateUpdate == null)
			{
				GlobalUserState = null;
			}
			else
			{
				GlobalUserState = new TwitchGlobalUserState(
					ExtractBadgeInfo(globalUserStateUpdate),
					ExtractBadges(globalUserStateUpdate),
					ExtractColor(globalUserStateUpdate),
					ExtractUserId(globalUserStateUpdate),
					ExtractDisplayName(globalUserStateUpdate),
					ExtractEmoteSets(globalUserStateUpdate)! // Will always contain at least "0"
				);
			}
		}

		void ITwitchUserStateTrackerService.UpdateUserState(string channelId, ReadOnlyDictionary<string, string>? userStateUpdate)
		{
			if (userStateUpdate == null)
			{
				_userStates.TryRemove(channelId, out _);
			}
			else if (_userStates.TryGetValue(channelId, out var existingUserState))
			{
				existingUserState.UpdateState(
					ExtractBadgeInfo(userStateUpdate),
					ExtractBadges(userStateUpdate),
					ExtractColor(userStateUpdate),
					ExtractUserId(userStateUpdate),
					ExtractDisplayName(userStateUpdate),
					ExtractEmoteSets(userStateUpdate)
				);
			}
			else
			{
				_userStates.TryAdd(channelId, new TwitchUserState(
					ExtractBadgeInfo(userStateUpdate),
					ExtractBadges(userStateUpdate),
					ExtractColor(userStateUpdate),
					ExtractUserId(userStateUpdate),
					ExtractDisplayName(userStateUpdate),
					ExtractEmoteSets(userStateUpdate)
				));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string? ExtractBadgeInfo(IReadOnlyDictionary<string, string> userStateUpdate)
		{
			return userStateUpdate.TryGetValue(IrcMessageTags.BADGE_INFO, out var badgeInfoString) ? badgeInfoString : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string? ExtractBadges(IReadOnlyDictionary<string, string> userStateUpdate)
		{
			return userStateUpdate.TryGetValue(IrcMessageTags.BADGES, out var badgesString) ? badgesString : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string? ExtractColor(IReadOnlyDictionary<string, string> userStateUpdate)
		{
			return userStateUpdate.TryGetValue(IrcMessageTags.COLOR, out var color) ? color : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string ExtractUserId(IReadOnlyDictionary<string, string> userStateUpdate)
		{
			return userStateUpdate.TryGetValue(IrcMessageTags.USER_ID, out var userId) ? userId : string.Empty;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string? ExtractDisplayName(IReadOnlyDictionary<string, string> userStateUpdate)
		{
			return userStateUpdate.TryGetValue(IrcMessageTags.DISPLAY_NAME, out var displayName) ? displayName : null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string? ExtractEmoteSets(IReadOnlyDictionary<string, string> userStateUpdate)
		{
			return userStateUpdate.TryGetValue(IrcMessageTags.EMOTE_SETS, out var emoteSets) ? emoteSets : null;
		}
	}
}