using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CatCore.Helpers;
using CatCore.Models.Shared;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal abstract class KittenPlatformServiceManagerBase<T> : IKittenPlatformServiceManagerBase where T : IPlatformService
	{
		private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

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

		public void Start(Assembly callingAssembly)
		{
			using var _ = Synchronization.Lock(_locker);
			RegisteredAssemblies.Add(callingAssembly);

			if (IsRunning)
			{
				return;
			}

			_activeStateManager.UpdateState(_platformType, false);
			_platformService.Start();

			_logger.Information("Started");
		}

		public void Stop(Assembly? callingAssembly)
		{
			using var _ = Synchronization.Lock(_locker);
			if (!IsRunning)
			{
				return;
			}

			if (callingAssembly != null)
			{
				RegisteredAssemblies.Remove(callingAssembly);
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
			_platformService.Stop();

			_logger.Information("Stopped");
		}

		public void Dispose()
		{
			if(IsRunning)
			{
				Stop(null!);
			}

			_logger.Information("Disposed");
		}

		public T GetService()
		{
			return _platformService;
		}
	}
}