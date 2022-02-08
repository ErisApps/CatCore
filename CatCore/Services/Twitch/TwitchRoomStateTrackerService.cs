using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Twitch.Interfaces;

namespace CatCore.Services.Twitch
{
	public sealed class TwitchRoomStateTrackerService : ITwitchRoomStateTrackerService
	{
		private readonly ConcurrentDictionary<string, TwitchRoomState> _roomStates;

		internal TwitchRoomStateTrackerService()
		{
			_roomStates = new ConcurrentDictionary<string, TwitchRoomState>();
		}

		/// <inheritdoc />
		public TwitchRoomState? GetRoomState(string channelName)
		{
			return _roomStates.TryGetValue(channelName, out var roomState) ? roomState : null;
		}

		// ReSharper disable once CognitiveComplexity
		TwitchRoomState? ITwitchRoomStateTrackerService.UpdateRoomState(string channelName, ReadOnlyDictionary<string, string>? roomStateUpdate)
		{
			if (roomStateUpdate == null)
			{
				return _roomStates.TryRemove(channelName, out var existingRoomState) ? existingRoomState : null;
			}
			else if (_roomStates.TryGetValue(channelName, out var existingRoomState))
			{
				foreach (var tag in roomStateUpdate)
				{
					switch (tag.Key)
					{
						case IrcMessageTags.ROOM_ID:
							existingRoomState.RoomId = tag.Value;
							break;
						case IrcMessageTags.EMOTE_ONLY:
							existingRoomState.EmoteOnly = tag.Value == "1";
							break;
						case IrcMessageTags.FOLLOWERS_ONLY:
							existingRoomState.FollowersOnly = tag.Value != "-1";
							existingRoomState.MinFollowTime = ParseMinimumFollowTime(tag.Value);
							break;
						case IrcMessageTags.SUBS_ONLY:
							existingRoomState.SubscribersOnly = tag.Value == "1";
							break;
						case IrcMessageTags.R9_K:
							existingRoomState.R9K = tag.Value == "1";
							break;
						case IrcMessageTags.SLOW:
							existingRoomState.SlowModeInterval = int.TryParse(tag.Value, out var slowModeInterval) ? slowModeInterval : 0;
							break;
					}
				}

				return existingRoomState;
			}
			else
			{
				var newRoomState = new TwitchRoomState(
					ExtractRoomId(roomStateUpdate),
					ExtractEmoteOnly(roomStateUpdate),
					ExtractFollowersOnly(roomStateUpdate, out var followersOnly),
					ExtractSubscribersOnly(roomStateUpdate),
					ExtractR9K(roomStateUpdate),
					ExtractSlowModeInterval(roomStateUpdate),
					ParseMinimumFollowTime(followersOnly));
				_roomStates.TryAdd(channelName, newRoomState);

				return newRoomState;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string ExtractRoomId(IReadOnlyDictionary<string, string> roomStateUpdate)
		{
			return roomStateUpdate.TryGetValue(IrcMessageTags.ROOM_ID, out var roomId) ? roomId : string.Empty;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ExtractEmoteOnly(IReadOnlyDictionary<string, string> roomStateUpdate)
		{
			return roomStateUpdate.TryGetValue(IrcMessageTags.EMOTE_ONLY, out var emoteOnly) && emoteOnly == "1";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ExtractFollowersOnly(IReadOnlyDictionary<string, string> roomStateUpdate, out string? followersOnly)
		{
			return roomStateUpdate.TryGetValue(IrcMessageTags.FOLLOWERS_ONLY, out followersOnly) && followersOnly != "-1";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ExtractSubscribersOnly(IReadOnlyDictionary<string, string> roomStateUpdate)
		{
			return roomStateUpdate.TryGetValue(IrcMessageTags.SUBS_ONLY, out var subsOnly) && subsOnly == "1";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ExtractR9K(IReadOnlyDictionary<string, string> roomStateUpdate)
		{
			return roomStateUpdate.TryGetValue(IrcMessageTags.R9_K, out var r9K) && r9K == "1";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ExtractSlowModeInterval(IReadOnlyDictionary<string, string> roomStateUpdate)
		{
			return roomStateUpdate.TryGetValue(IrcMessageTags.SLOW, out var slow) && int.TryParse(slow, out var slowModeInterval) ? slowModeInterval : 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ParseMinimumFollowTime(string? followersOnly)
		{
			return followersOnly != "-1" && int.TryParse(followersOnly, out var minFollowTime) ? minFollowTime : 0;
		}
	}
}