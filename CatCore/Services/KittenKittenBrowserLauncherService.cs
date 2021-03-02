using System.Diagnostics;
using CatCore.Services.Interfaces;

namespace CatCore.Services
{
	internal class KittenKittenBrowserLauncherService : IKittenBrowserLauncherService
	{
		public void Launch(string uri)
		{
			Process.Start(uri);
		}
	}
}