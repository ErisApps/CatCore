using System;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	public class TwitchService : ITwitchService
	{
		private readonly ILogger _logger;
		private readonly ITwitchHelixApiService _twitchHelixApiService;

		internal TwitchService(ILogger logger, ITwitchHelixApiService twitchHelixApiService)
		{
			_logger = logger;
			_twitchHelixApiService = twitchHelixApiService;
		}

		public ITwitchHelixApiService GetHelixApiService() => _twitchHelixApiService;

		void IPlatformService.Start()
		{
			// TODO: Implement this
		}

		void IPlatformService.Stop()
		{
			// TODO: Implement this
		}
	}
}