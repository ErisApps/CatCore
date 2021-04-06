using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using CatCore.Helpers;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal abstract class KittenPlatformServiceManagerBase<T> : IDisposable where T : IPlatformService
	{
		private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

		private readonly ILogger _logger;
		private readonly T _platformService;

		internal HashSet<Assembly> RegisteredAssemblies { get; }

		internal KittenPlatformServiceManagerBase(ILogger logger, T platformService)
		{
			_logger = logger;
			_platformService = platformService;

			RegisteredAssemblies = new HashSet<Assembly>();
		}

		public bool IsRunning { get; private set; }

		internal void Start(Assembly callingAssembly)
		{
			using var _ = Synchronization.Lock(_locker);
			RegisteredAssemblies.Add(callingAssembly);

			if (IsRunning)
			{
				return;
			}

			_platformService.Start();
			IsRunning = true;

			_logger.Information("Started");
		}

		internal void Stop(Assembly? callingAssembly)
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

			_platformService.Stop();
			IsRunning = false;

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