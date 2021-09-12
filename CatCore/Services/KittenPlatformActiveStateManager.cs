using System.Collections.Concurrent;
using CatCore.Models.Shared;
using CatCore.Services.Interfaces;

namespace CatCore.Services
{
	internal sealed class KittenPlatformActiveStateManager : IKittenPlatformActiveStateManager
	{
		private readonly ConcurrentDictionary<PlatformType, bool> _platformActiveStates;

		public KittenPlatformActiveStateManager()
		{
			_platformActiveStates = new ConcurrentDictionary<PlatformType, bool>();
		}

		public bool GetState(PlatformType platformType)
		{
			return _platformActiveStates.ContainsKey(platformType);
		}

		public void UpdateState(PlatformType platformType, bool active)
		{
			if (active)
			{
				_platformActiveStates.AddOrUpdate(platformType, active, (_, _) => active);
			}
			else
			{
				_platformActiveStates.TryRemove(platformType, out _);
			}
		}
	}
}