using System;
using System.Reflection;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenPlatformServiceManagerBase : IDisposable
	{
		bool IsRunning { get; }
		void Start(Assembly callingAssembly);
		void Stop(Assembly? callingAssembly);
	}
}