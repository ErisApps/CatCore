using CatCore.Models.Credentials;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal class TwitchCredentialsProvider : KittenCredentialsProvider<TwitchCredentials>, ITwitchCredentialsProvider
	{
		private const string SERVICE_TYPE = nameof(Twitch);

		protected override string ServiceType => SERVICE_TYPE;

		public TwitchCredentialsProvider(ILogger logger, IKittenPathProvider pathProvider) : base(logger, pathProvider)
		{
		}
	}
}