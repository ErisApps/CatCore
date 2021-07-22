using CatCore.Models.Config;

namespace CatCore.Models.Api.Responses
{
	internal readonly struct GlobalStateResponseDto
	{
		public bool LaunchInternalApiOnStartup { get; }
		public bool LaunchWebPortalOnStartup { get; }
		public bool ParseEmojis { get; }

		public GlobalStateResponseDto(GlobalConfig globalConfig)
		{
			LaunchInternalApiOnStartup = globalConfig.LaunchInternalApiOnStartup;
			LaunchWebPortalOnStartup = globalConfig.LaunchWebPortalOnStartup;
			ParseEmojis = globalConfig.HandleEmojis;
		}
	}
}