using System.Collections.Generic;
using System.Reflection;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services.Multiplexer
{
	internal class ChatServiceMultiplexerManager : IKittenPlatformServiceManagerBase
	{
		private readonly ILogger _logger;
		private readonly ChatServiceMultiplexer _chatServiceMultiplexer;
		private readonly IList<IKittenPlatformServiceManagerBase> _platformServices;

		public ChatServiceMultiplexerManager(ILogger logger, ChatServiceMultiplexer chatServiceMultiplexer, IList<IKittenPlatformServiceManagerBase> platformServices)
		{
			_logger = logger;
			_chatServiceMultiplexer = chatServiceMultiplexer;
			_platformServices = platformServices;
		}

		public bool IsRunning => false;

		public void Start(Assembly callingAssembly)
		{
			foreach (var service in _platformServices)
			{
				service.Start(callingAssembly);
			}

			_logger.Information("Streaming services have been started");
		}

		public void Stop(Assembly? callingAssembly)
		{
			foreach (var service in _platformServices)
			{
				service.Stop(callingAssembly);
			}

			_logger.Information("Streaming services have been stopped");
		}

		public void Dispose()
		{
			foreach (var service in _platformServices)
			{
				service.Stop(null);
			}

			_logger.Information("Disposed");
		}

		public ChatServiceMultiplexer GetMultiplexer()
		{
			return _chatServiceMultiplexer;
		}
	}
}