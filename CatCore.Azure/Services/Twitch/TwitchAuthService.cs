using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CatCore.Azure.Services.Twitch
{
	internal class TwitchAuthService
	{
		private const string TWITCH_AUTH_BASEURL = "https://id.twitch.tv/oauth2/";

		private readonly HttpClient _authClient;

		public TwitchAuthService(IHttpClientFactory httpClientFactory)
		{
			_authClient = httpClientFactory.CreateClient();
			_authClient.DefaultRequestVersion = HttpVersion.Version20;
			_authClient.BaseAddress = new Uri(TWITCH_AUTH_BASEURL, UriKind.Absolute);
			_authClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(CatCore)}/1.0.0");
		}

		public async Task<Stream?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl)
		{
			var request = new HttpRequestMessage(HttpMethod.Post, $"{TWITCH_AUTH_BASEURL}token" +
			                                                      $"?client_id={Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId")}" +
			                                                      $"&client_secret={Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret")}" +
			                                                      $"&code={authorizationCode}" +
			                                                      "&grant_type=authorization_code" +
			                                                      $"&redirect_uri={redirectUrl}");
			using var responseMessage = await _authClient
				.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				return null;
			}

			return await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
		}

		public async Task<Stream?> RefreshTokens(string refreshToken)
		{
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				return null;
			}

			var request = new HttpRequestMessage(HttpMethod.Post, $"{TWITCH_AUTH_BASEURL}token" +
			                                                      $"?client_id={Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId")}" +
			                                                      $"&client_secret={Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret")}" +
			                                                      "&grant_type=refresh_token" +
			                                                      $"&refresh_token={refreshToken}");

			using var responseMessage = await _authClient
				.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				return null;
			}

			return await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
		}
	}
}