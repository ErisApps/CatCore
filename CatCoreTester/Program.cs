using System;
using System.Threading.Tasks;
using CatCore;
using CatCore.Services.Twitch;
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
			Console.WriteLine("WARNING: It also currently relies on in-memory access and refresh tokens in the TwitchAuthService, so make sure to login first before continue-ing.");
			Console.WriteLine();

			var twitchAuthService = (TwitchAuthService) chatCoreInstance.Container!.Resolve<ITwitchAuthService>();

			async Task CheckTokenValidity()
			{
				var validationResponse = await twitchAuthService.ValidateAccessToken().ConfigureAwait(false);
				Console.WriteLine(validationResponse != null ? $"Token valid until {DateTimeOffset.Now.AddSeconds(validationResponse.Value.ExpiresIn)}" : "Token has expired sadly...  D:");
				Console.WriteLine();
			}

			while (Console.ReadKey().KeyChar != 'c')
			{
			}
			Console.WriteLine();
			Console.WriteLine();

			Console.WriteLine("Checking current access token status.");
			await CheckTokenValidity().ConfigureAwait(false);

			var oldAccessToken = twitchAuthService.AccessToken;

			Console.WriteLine("Initiating token refresh... Please stand by.");
			var tokenRefreshSuccess = await twitchAuthService.RefreshTokens().ConfigureAwait(false);
			Console.WriteLine($"Refresh successful: {tokenRefreshSuccess}");
			Console.WriteLine();

			Console.WriteLine("Checking new access token status.");
			await CheckTokenValidity().ConfigureAwait(false);

			var newAccessToken = twitchAuthService.AccessToken;

			Console.WriteLine("Initiating token revocation process... Please stand by.");
			var revokeTokenSuccess = await twitchAuthService.RevokeTokens().ConfigureAwait(false);
			Console.WriteLine($"Revocation successful: {revokeTokenSuccess}");
			Console.WriteLine();

			Console.WriteLine("Checking new access token validity...");
			twitchAuthService.AccessToken = newAccessToken;
			await CheckTokenValidity().ConfigureAwait(false);

			Console.WriteLine("Checking old access token validity...");
			twitchAuthService.AccessToken = oldAccessToken;
			await CheckTokenValidity().ConfigureAwait(false);

			await Task.Delay(-1).ConfigureAwait(false);
		}
	}
}