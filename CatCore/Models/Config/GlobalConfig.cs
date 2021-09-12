namespace CatCore.Models.Config
{
	internal sealed class GlobalConfig
	{
		public bool LaunchInternalApiOnStartup { get; set; } = true;
		public bool LaunchWebPortalOnStartup { get; set; } = true;

		public bool HandleEmojis { get; set; } = true;
	}
}