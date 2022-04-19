using System;
using System.Linq;
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
			var chatCoreInstance = CatCoreInstance.Create(
#if !DEBUG
				(level, context, message) => Console.Write($"External logger: {DateTimeOffset.Now:O}|{level}|{context}|{message}")
#endif
			);

			stoppyWatch.Stop();

			Console.WriteLine($"Tester finished. Instance creation time: {(double)stoppyWatch.ElapsedTicks/TimeSpan.TicksPerMillisecond}ms");
			Console.WriteLine();
			Console.WriteLine("========================");
			Console.WriteLine("WARNING: Test below will only work in DEBUG mode due to the IoC container being exposed only in that build mode.");
			Console.WriteLine("WARNING: It also currently relies on in-memory access and refresh tokens in the TwitchAuthService, so make sure to login first before continue-ing.");
			Console.WriteLine();
			Console.WriteLine("Press \"c\" to continue...");

			chatCoreInstance.LaunchWebPortal();

			while (Console.ReadKey().KeyChar != 'c')
			{
				await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
			}

			var chatServiceMultiplexer = chatCoreInstance.RunAllServices();
			chatServiceMultiplexer.OnChatConnected += async service =>
			{
				await Console.Out.WriteLineAsync("Logged in... presumably").ConfigureAwait(false);
				if (service.Underlying is TwitchService twitchService)
				{
					var helix = twitchService.GetHelixApiService();

					/*var pollHistory = await helix.GetPolls().ConfigureAwait(false);

					var pollData = await helix.CreatePoll("Is this a CatCore generated poll?", new List<string> {"Yes!!!", "No?"}, 60, channelPointsVotingEnabled: true, channelPointsPerVote: 15).ConfigureAwait(false);
					// var pollData = await helix.CreatePoll("CatCore: Should Eris stay awake?", new List<string> {"Yes!!!", "No?"}, 60).ConfigureAwait(false);
					var first = pollData.Value.Data.First();
					var firstStatus = first.Status;
					var firstStartedAt = first.StartedAt;


					var terminatedResult = await helix.EndPoll(first.Id, PollStatus.Terminated).ConfigureAwait(false);*/


					/*var predictionHistory = await helix.GetPredictions().ConfigureAwait(false);
					var lastPrediction = predictionHistory.Value.Data.First();
					if (lastPrediction.Status == PredictionStatus.Active || lastPrediction.Status == PredictionStatus.Locked)
					{
						await helix.EndPrediction(lastPrediction.Id, PredictionStatus.Cancelled);
					}

					var predictionData = await helix.CreatePrediction("CatCore: Will someone vote on this?", new List<string> {"Yes!!!", "No?"}, 60).ConfigureAwait(false);
					var first = predictionData.Value.Data.First();*/


					// await helix.EndPrediction(first.Id, PredictionStatus.Cancelled).ConfigureAwait(false);

					Console.WriteLine("Can't touch this.");
				}
			};



			/*

			var sema = new SemaphoreSlim(1, 1);

			async void OnChatServiceMultiplexerOnOnJoinChannel(IPlatformService service, IChatChannel channel)
			{
				if (channel.Id != twitchChannel.Id)
				{
					return;
				}

				if (!await sema.WaitAsync(0).ConfigureAwait(false))
				{
					return;
				}

				/*await Task.Delay(TimeSpan.FromSeconds(35)).ConfigureAwait(false);

				for (var i = 0; i < 10; i++)
				{
					chatServiceMultiplexer.SendMessage(channel, "!bomb");

					await Task.Delay(TimeSpan.FromSeconds(35)).ConfigureAwait(false);
				}#1#

				/*const int count = 1;

				chatServiceMultiplexer.SendMessage(twitchChannel, $"Testing being conform with rate-limit. Message -01. Initial message... {count} msg burst will start in roughly 10 seconds from now.");

				await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

				for (var i = 0; i < count; i++)
				{
					chatServiceMultiplexer.SendMessage(twitchChannel, $"Testing being conform with rate-limit. Message {i + 1:000}");
				}#1#
			}

			chatServiceMultiplexer.OnJoinChannel += OnChatServiceMultiplexerOnOnJoinChannel;
			*/

			// await Task.Delay(5000).ConfigureAwait(false);
			// chatCoreInstance.StopTwitchServices();

			while (Console.ReadKey().KeyChar != 'c')
			{
				await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
			}

			Console.WriteLine();
			Console.WriteLine();

#if DEBUG
			var twitchAuthService = chatCoreInstance.Resolver!.Resolve<ITwitchAuthService>();
			var twitchHelixApiService = chatCoreInstance.Resolver!.Resolve<ITwitchHelixApiService>();

			/*async Task CheckTokenValidity()
			{
				var validationResponse = await twitchAuthService.ValidateAccessToken().ConfigureAwait(false);
				Console.WriteLine(validationResponse != null ? $"Token valid until {validationResponse.Value.ExpiresIn}" : "Token has expired sadly...  D:");
				Console.WriteLine();
			}

			Console.WriteLine("Checking current access token status.");
			await CheckTokenValidity().ConfigureAwait(false);*/

			var oldAccessToken = twitchAuthService.AccessToken;
			//var oldRefreshToken = twitchAuthService.RefreshToken;

			Console.WriteLine("Initiating token refresh... Please stand by.");
			var tokenRefreshSuccess = await twitchAuthService.RefreshTokens().ConfigureAwait(false);
			Console.WriteLine($"Refresh successful: {tokenRefreshSuccess}");
			Console.WriteLine();

			Console.WriteLine("Checking new access token status.");
			// await CheckTokenValidity().ConfigureAwait(false);

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