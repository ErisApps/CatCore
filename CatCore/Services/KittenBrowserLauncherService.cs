using System.Diagnostics;
using CatCore.Services.Interfaces;

namespace CatCore.Services
{
	internal class KittenBrowserLauncherService : IKittenBrowserLauncherService
	{
		public void Launch(string uri)
		{
			Process.Start(uri);
		}
	}
}