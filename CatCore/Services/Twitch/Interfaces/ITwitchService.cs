using CatCore.Services.Interfaces;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchService : IPlatformService
	{
		ITwitchHelixApiService GetHelixApiService();
	}
}