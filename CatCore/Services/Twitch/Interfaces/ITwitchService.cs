using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Interfaces;
using JetBrains.Annotations;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchService : IPlatformService<ITwitchService, TwitchChannel, TwitchMessage>
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