using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenPlatformServiceManagerBase : IDisposable
	{
		bool IsRunning { get; }
		Task Start(Assembly callingAssembly);
		Task Stop(Assembly? callingAssembly);
	}
}