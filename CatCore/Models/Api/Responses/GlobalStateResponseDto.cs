using CatCore.Models.Config;

namespace CatCore.Models.Api.Responses
{
	internal readonly struct GlobalStateResponseDto
	{
		public bool LaunchWebAppOnStart { get; }
		public bool ParseEmojis { get; }

		public GlobalStateResponseDto(GlobalConfig globalConfig)
		{
			LaunchWebAppOnStart = globalConfig.LaunchWebAppOnStartup;
			ParseEmojis = globalConfig.HandleEmojis;
		}
	}
}