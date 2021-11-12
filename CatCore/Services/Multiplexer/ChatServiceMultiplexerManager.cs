using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services.Multiplexer
{
	internal sealed class ChatServiceMultiplexerManager : IKittenPlatformServiceManagerBase
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

		public async Task Start(Assembly callingAssembly)
		{
			foreach (var service in _platformServices)
			{
				await service.Start(callingAssembly);
			}

			_logger.Information("Streaming services have been started");
		}

		public async Task Stop(Assembly? callingAssembly)
		{
			foreach (var service in _platformServices)
			{
				await service.Stop(callingAssembly);
			}

			_logger.Information("Streaming services have been stopped");
		}

		public void Dispose()
		{
			foreach (var service in _platformServices)
			{
				// TODO: how do you want to handle this?
				_ = service.Stop(null);
			}

			_logger.Information("Disposed");
		}

		public ChatServiceMultiplexer GetMultiplexer()
		{
			return _chatServiceMultiplexer;
		}
	}
}