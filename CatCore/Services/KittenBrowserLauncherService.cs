using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CatCore.Services.Interfaces;

namespace CatCore.Services
{
	internal sealed class KittenBrowserLauncherService : IKittenBrowserLauncherService
	{
		public void LaunchWebPortal()
		{
			Task.Run(() =>
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					Process.Start(ConstantsBase.InternalApiServerUri);
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					Process.Start("xdg-open", ConstantsBase.InternalApiServerUri);
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("open", ConstantsBase.InternalApiServerUri);
				}
			});
		}
	}
}