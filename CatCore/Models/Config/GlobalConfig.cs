namespace CatCore.Models.Config
{
	internal class GlobalConfig
	{
		public bool LaunchInternalApiOnStartup { get; set; } = true;
		public bool LaunchWebPortalOnStartup { get; set; } = true;

		public bool HandleEmojis { get; set; } = true;
	}
}