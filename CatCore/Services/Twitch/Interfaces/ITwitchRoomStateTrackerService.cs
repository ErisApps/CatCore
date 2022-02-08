using System.Collections.ObjectModel;
using CatCore.Models.Twitch.IRC;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchRoomStateTrackerService
	{
		/// <summary>
		/// Returns the RoomState for the specified channelName.
		/// </summary>
		/// <remarks>This is only available when the channel is successfully joined over IRC</remarks>
		/// <returns>RoomState for the specified channel</returns>
		[PublicAPI]
		TwitchRoomState? GetRoomState(string channelName);

		internal TwitchRoomState? UpdateRoomState(string channelName, ReadOnlyDictionary<string, string>? roomStateUpdate);
	}
}