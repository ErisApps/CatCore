using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	public class TwitchService : ITwitchService
	{
		private readonly ILogger _logger;
		private readonly ITwitchIrcService _twitchIrcService;
		private readonly ITwitchHelixApiService _twitchHelixApiService;

		internal TwitchService(ILogger logger, ITwitchIrcService twitchIrcService, ITwitchHelixApiService twitchHelixApiService)
		{
			_logger = logger;
			_twitchIrcService = twitchIrcService;
			_twitchHelixApiService = twitchHelixApiService;
		}

		public IChatService GetChatService() => _twitchIrcService;
		public ITwitchHelixApiService GetHelixApiService() => _twitchHelixApiService;

		void IPlatformService.Start()
		{
			_logger.Information("Initializing {Type}", nameof(TwitchService));
			_twitchIrcService.Start();
		}

		void IPlatformService.Stop()
		{
			_logger.Information("Stopped {Type}", nameof(TwitchService));
			_twitchIrcService.Stop();
		}
	}
}