using CatCore.Models.Shared;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchServiceManager : KittenPlatformServiceManagerBase<ITwitchService, TwitchChannel, TwitchMessage>
	{
		public TwitchServiceManager(ILogger logger, ITwitchService twitchService, IKittenPlatformActiveStateManager activeStateManager)
			: base(logger, twitchService, activeStateManager, PlatformType.Twitch)
		{
		}
	}
}