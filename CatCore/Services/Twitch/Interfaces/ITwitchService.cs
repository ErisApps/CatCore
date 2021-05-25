using CatCore.Services.Interfaces;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchService : IPlatformService
	{
		[PublicAPI]
		ITwitchPubSubServiceManager GetPubSubService();

		[PublicAPI]
		ITwitchHelixApiService GetHelixApiService();

		[PublicAPI]
		ITwitchRoomStateTrackerService GetRoomStateTrackerService();

		[PublicAPI]
		ITwitchUserStateTrackerService GetUserStateTrackerService();

		[PublicAPI]
		ITwitchChannelManagementService GetChannelManagementService();
	}
}