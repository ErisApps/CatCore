using CatCore.Models.Shared;

namespace CatCore.Services.Interfaces
{
	internal interface IKittenPlatformActiveStateManager
	{
		bool GetState(PlatformType platformType);
		void UpdateState(PlatformType platformType, bool active);
	}
}