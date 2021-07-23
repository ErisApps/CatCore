using System;
using System.Linq;
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
			var chatCoreInstance = ChatCoreInstance.CreateInstance(
#if !DEBUG
				(level, context, message) => Console.Write($"External logger: {level}|{context}|{message}")
#endif
			);

			stoppyWatch.Stop();

			Console.WriteLine($"Tester finished. Instance creation time: {stoppyWatch.Elapsed:g}");
			Console.WriteLine();
			Console.WriteLine("========================");
			Console.WriteLine("WARNING: Test below will only work in DEBUG mode due to the IoC container being exposed only in that build mode.");
			Console.WriteLine("WARNING: It also currently relies on in-memory access and refresh tokens in the TwitchAuthService, so make sure to login first before continue-ing.");
			Console.WriteLine();
			Console.WriteLine("Press \"c\" to continue...");

			while (Console.ReadKey().KeyChar != 'c')
			{
				await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
			}

			Console.WriteLine();
			Console.WriteLine();

#if DEBUG
			var twitchAuthService = chatCoreInstance.Container!.Resolve<ITwitchAuthService>();
			var twitchHelixApiService = chatCoreInstance.Container!.Resolve<ITwitchHelixApiService>();

			async Task CheckTokenValidity()
			{
				var validationResponse = await twitchAuthService.ValidateAccessToken().ConfigureAwait(false);
				Console.WriteLine(validationResponse != null ? $"Token valid until {validationResponse.Value.ExpiresIn}" : "Token has expired sadly...  D:");
				Console.WriteLine();
			}

			Console.WriteLine("Checking current access token status.");
			await CheckTokenValidity().ConfigureAwait(false);

			var oldAccessToken = twitchAuthService.AccessToken;
			//var oldRefreshToken = twitchAuthService.RefreshToken;

			Console.WriteLine("Initiating token refresh... Please stand by.");
			var tokenRefreshSuccess = await twitchAuthService.RefreshTokens().ConfigureAwait(false);
			Console.WriteLine($"Refresh successful: {tokenRefreshSuccess}");
			Console.WriteLine();

			Console.WriteLine("Checking new access token status.");
			await CheckTokenValidity().ConfigureAwait(false);

			Console.WriteLine("Requesting some data through Helix");
			var userInfoResponse = await twitchHelixApiService.FetchUserInfo(loginNames: new[] {"realeris"}).ConfigureAwait(false);

			Console.WriteLine("Search channels through Helix");
			var channelData = await twitchHelixApiService.SearchChannels("realeris").ConfigureAwait(false);
			var ccc = channelData.Value.Data.FirstOrDefault(x => x.BroadcasterLogin == "realeris");
			Console.WriteLine(ccc.StartedAt);

			/*Console.WriteLine("Creating stream marker through Helix without a description. (Will fail when stream on the requested channel is offline.)");
			var streamMarkerResponse = await twitchHelixApiService.CreateStreamMarker(userInfoResponse!.Value.Data[0].UserId).ConfigureAwait(false);
			Console.WriteLine();

			Console.WriteLine("Creating stream marker through Helix with a description. (Will fail when stream on the requested channel is offline.)");
			var streamMarkerResponse2 = await twitchHelixApiService.CreateStreamMarker(userInfoResponse!.Value.Data[0].UserId, "Erm... is this thing on?").ConfigureAwait(false);
			Console.WriteLine();

			var newAccessToken = twitchCredentialsProvider.Credentials.AccessToken;
			var newRefreshToken = twitchCredentialsProvider.Credentials.RefreshToken;

			Console.WriteLine("Initiating token revocation process... Please stand by.");
			var revokeTokenSuccess = await twitchAuthService.RevokeTokens().ConfigureAwait(false);
			Console.WriteLine($"Revocation successful: {revokeTokenSuccess}");
			Console.WriteLine();

			Console.WriteLine("Checking new access token validity...");
			twitchCredentialsProvider.Credentials.AccessToken = newAccessToken;
			twitchCredentialsProvider.Credentials.RefreshToken = newRefreshToken;
			await CheckTokenValidity().ConfigureAwait(false);

			Console.WriteLine("Checking old access token validity...");
			twitchCredentialsProvider.Credentials.AccessToken = oldAccessToken;
			twitchCredentialsProvider.Credentials.RefreshToken = oldRefreshToken;
			await CheckTokenValidity().ConfigureAwait(false);*/
#endif

			await Task.Delay(-1).ConfigureAwait(false);
		}
	}
}