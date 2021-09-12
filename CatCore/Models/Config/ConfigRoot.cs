namespace CatCore.Models.Config
{
	internal sealed class ConfigRoot
	{
		public GlobalConfig GlobalConfig { get; set; }
		public TwitchConfig TwitchConfig { get; set; }

		public ConfigRoot()
		{
			GlobalConfig = new GlobalConfig();
			TwitchConfig = new TwitchConfig();
		}
	}
}