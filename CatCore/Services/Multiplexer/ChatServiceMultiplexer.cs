using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Multiplexer
{
	public class ChatServiceMultiplexer : KittenChatServiceBase
	{
		private readonly ILogger _logger;
		private readonly IList<IPlatformService> _platformServices;

		private readonly Assembly _ownAssembly;

		public ChatServiceMultiplexer(ILogger logger, IList<IPlatformService> platformServices, Assembly ownAssembly)
		{
			_logger = logger;
			_platformServices = platformServices;

			_ownAssembly = ownAssembly;

			foreach (var platformService in _platformServices)
			{
				var chatService = platformService.GetChatService();

				// TODO: Register to all event handlers of IChatService
			}
		}

		public ITwitchService GetTwitchPlatformService()
		{
			return _platformServices.OfType<ITwitchService>().First();
		}
	}
}