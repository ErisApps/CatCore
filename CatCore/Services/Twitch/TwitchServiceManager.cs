using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal class TwitchServiceManager : KittenPlatformServiceManagerBase<ITwitchService>
	{
		public TwitchServiceManager(ILogger logger, ITwitchService twitchService) : base(logger, twitchService)
		{
		}
	}
}