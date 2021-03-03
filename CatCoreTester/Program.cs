using System;
using System.Threading.Tasks;
using CatCore;
using CatCore.Services.Twitch.Interfaces;
using DryIoc;

namespace CatCoreTester
{
	internal static class Program
	{
		private static async Task Main(string[] args)
		{
			var stoppyWatch = new System.Diagnostics.Stopwatch();
			Console.WriteLine("Tester init");

			stoppyWatch.Start();
			var chatCoreInstance = ChatCoreInstance.CreateInstance((level, message) => Console.WriteLine($"External logger: {message}"));
			stoppyWatch.Stop();

			Console.WriteLine($"Tester finished. Instance creation time: {stoppyWatch.Elapsed:g}");
			Console.WriteLine();
			Console.WriteLine("========================");
			Console.WriteLine("WARNING: Test below will only work in DEBUG mode due to the IoC container being exposed only in that build mode.");
			Console.WriteLine("WARNING: It also currently relies on hard-coded access and refresh tokens in the TwitchAuthService.");
			Console.WriteLine();

			var twitchAuthService = chatCoreInstance.Container!.Resolve<ITwitchAuthService>();

			async Task CheckTokenValidity()
			{
				var validationResponse = await twitchAuthService.ValidateAccessToken().ConfigureAwait(false);
				Console.WriteLine(validationResponse != null ? $"Token valid until {DateTimeOffset.Now.AddSeconds(validationResponse.Value.ExpiresIn)}" : "Token has expired sadly...  D:");
				Console.WriteLine();
			}

			await CheckTokenValidity().ConfigureAwait(false);

			Console.WriteLine("Initiating token refresh... Please stand by.");
			var tokenRefreshSuccess = await twitchAuthService.RefreshTokens().ConfigureAwait(false);
			Console.WriteLine($"Refresh successful: {tokenRefreshSuccess}");
			Console.WriteLine();

			await CheckTokenValidity().ConfigureAwait(false);

			await Task.Delay(-1).ConfigureAwait(false);
		}
	}
}