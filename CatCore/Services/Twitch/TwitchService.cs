using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	public class TwitchService
	{
		private readonly ILogger _logger;
		private readonly ITwitchHelixApiService _twitchHelixApiService;

		internal TwitchService(ILogger logger, ITwitchHelixApiService twitchHelixApiService)
		{
			_logger = logger;
			_twitchHelixApiService = twitchHelixApiService;
		}

		public ITwitchHelixApiService HelixApiService => _twitchHelixApiService;
	}
}