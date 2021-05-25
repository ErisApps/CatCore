using System.Diagnostics;
using System.Threading.Tasks;
using CatCore.Services.Interfaces;

namespace CatCore.Services
{
	internal class KittenBrowserLauncherService : IKittenBrowserLauncherService
	{
		public void LaunchWebPortal()
		{
			Task.Run(() => Process.Start(ConstantsBase.InternalApiServerUri));
		}
	}
}