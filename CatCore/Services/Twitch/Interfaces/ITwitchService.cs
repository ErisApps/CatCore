using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Interfaces;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchService : IPlatformService<ITwitchService, TwitchChannel, TwitchMessage>
	{
		/// <summary>
		///	Returns the PubSub service manager. Allows you to subscribe to various events.
		/// </summary>
		/// <returns>Returns the PubSub service manager</returns>
		[PublicAPI]
		ITwitchPubSubServiceManager GetPubSubService();

		/// <summary>
		/// Returns the Helix API service. Allows you to interact with the Twitch Helix API.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		ITwitchHelixApiService GetHelixApiService();

		/// <summary>
		///	Returns the RoomState tracker service. Keeps track of the state of the currently subscribed channels.
		/// </summary>
		/// <returns>Returns the RoomState tracker service</returns>
		[PublicAPI]
		ITwitchRoomStateTrackerService GetRoomStateTrackerService();

		/// <summary>
		/// Returns the UserState tracker service. Keeps track of the state of the user, both global and channel-specific.
		/// </summary>
		/// <returns>Returns the UserState tracker service</returns>
		[PublicAPI]
		ITwitchUserStateTrackerService GetUserStateTrackerService();

		/// <summary>
		/// Returns the Channel management service. Keeps track of all channels that were registered through the webportal.
		/// </summary>
		/// <returns>Returns the Channel management service</returns>
		[PublicAPI]
		ITwitchChannelManagementService GetChannelManagementService();
	}
}