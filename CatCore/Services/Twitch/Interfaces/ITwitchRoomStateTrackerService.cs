using System.Collections.ObjectModel;
using CatCore.Models.Twitch.IRC;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchRoomStateTrackerService
	{
		[PublicAPI]
		TwitchRoomState? GetRoomState(string channelName);

		internal void UpdateRoomState(string channelName, ReadOnlyDictionary<string, string>? roomStateUpdate);
	}
}