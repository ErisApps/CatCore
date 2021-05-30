using CatCore.Azure.Services.Twitch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CatCore.Azure
{
	public class Program
	{
		public static void Main()
		{
			var host = new HostBuilder()
				.ConfigureFunctionsWorkerDefaults()
				.ConfigureServices(builder =>
				{
					builder.AddHttpClient();
					builder.AddSingleton<TwitchAuthService>();
				})
				.Build();

			host.Run();
		}
	}
}