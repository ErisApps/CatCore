using CatCore.Models.Shared;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal class TwitchServiceManager : KittenPlatformServiceManagerBase<ITwitchService>
	{
		public TwitchServiceManager(ILogger logger, ITwitchService twitchService, IKittenPlatformActiveStateManager activeStateManager)
			: base(logger, twitchService, activeStateManager, PlatformType.Twitch)
		{
		}
	}
}