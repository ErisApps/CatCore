using System.Collections.ObjectModel;
using CatCore.Models.Twitch.IRC;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchUserStateTrackerService
	{
		[PublicAPI]
		TwitchGlobalUserState? GlobalUserState { get; }

		[PublicAPI]
		TwitchUserState? GetUserState(string channelId);

		internal void UpdateGlobalUserState(ReadOnlyDictionary<string, string>? globalUserStateUpdate);
		internal void UpdateUserState(string channelId, ReadOnlyDictionary<string, string>? userStateUpdate);
	}
}