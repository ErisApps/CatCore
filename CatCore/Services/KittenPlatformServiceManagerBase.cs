using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.Shared;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal abstract class KittenPlatformServiceManagerBase<T, TChannel, TMessage> : IKittenPlatformServiceManagerBase
		where T : IPlatformService<T, TChannel, TMessage>
		where TChannel : IChatChannel<TChannel, TMessage>
		where TMessage : IChatMessage<TMessage, TChannel>
	{
		private readonly SemaphoreSlim _locker = new(1, 1);

		private readonly ILogger _logger;
		private readonly T _platformService;
		private readonly IKittenPlatformActiveStateManager _activeStateManager;
		private readonly PlatformType _platformType;

		internal HashSet<Assembly> RegisteredAssemblies { get; }

		internal KittenPlatformServiceManagerBase(ILogger logger, T platformService, IKittenPlatformActiveStateManager activeStateManager, PlatformType platformType)
		{
			_logger = logger;
			_platformService = platformService;
			_activeStateManager = activeStateManager;
			_platformType = platformType;

			RegisteredAssemblies = new HashSet<Assembly>();
		}

		public bool IsRunning => _activeStateManager.GetState(_platformType);

		public async Task Start(Assembly callingAssembly)
		{
			using var __ = await Synchronization.LockAsync(_locker);
			_ = RegisteredAssemblies.Add(callingAssembly);

			if (IsRunning)
			{
				return;
			}

			_activeStateManager.UpdateState(_platformType, true);
			await _platformService.Start();

			_logger.Information("Started");
		}

		public async Task Stop(Assembly? callingAssembly)
		{
			using var __ = await Synchronization.LockAsync(_locker);
			if (!IsRunning)
			{
				return;
			}

			if (callingAssembly != null)
			{
				_ = RegisteredAssemblies.Remove(callingAssembly);
				if (RegisteredAssemblies.Any())
				{
					return;
				}
			}
			else
			{
				RegisteredAssemblies.Clear();
			}

			_activeStateManager.UpdateState(_platformType, false);
			await _platformService.Stop();

			_logger.Information("Stopped");
		}

		public void Dispose()
		{
			if (IsRunning)
			{
				// TODO: figure out how you want to handle this
				_ = Stop(null!);
			}

			_logger.Information("Disposed");
		}

		public T GetService()
		{
			return _platformService;
		}
	}
}