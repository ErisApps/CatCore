using System.Diagnostics;
using CatCore.Services.Interfaces;

namespace CatCore.Services
{
	internal class KittenBrowserLauncherService : IKittenBrowserLauncherService
	{
		private readonly IKittenApiService _kittenApiService;

		/// <remark>
		/// The IKittenApiService implementation is injected to ensure the api is actually up-and-running.
		/// Injecting it for the first time will run the init logic.
		/// </remark>
		public KittenBrowserLauncherService(IKittenApiService kittenApiService)
		{
			_kittenApiService = kittenApiService;
		}

		public void LaunchWebPortal()
		{
			Process.Start(_kittenApiService.ServerUri);
		}
	}
}