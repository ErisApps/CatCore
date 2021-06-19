using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CatCore.Shared.Models.Twitch.OAuth;
using Microsoft.Extensions.Logging;

namespace CatCore.Azure.Services.Twitch
{
	internal class TwitchAuthService
	{
		private const string TWITCH_AUTH_BASEURL = "https://id.twitch.tv/oauth2/";

		private readonly ILogger<TwitchAuthService> _logger;
		private readonly HttpClient _authClient;

		public TwitchAuthService(ILogger<TwitchAuthService> logger, IHttpClientFactory httpClientFactory)
		{
			_logger = logger;

			_authClient = httpClientFactory.CreateClient();
			_authClient.DefaultRequestVersion = HttpVersion.Version20;
			_authClient.BaseAddress = new Uri(TWITCH_AUTH_BASEURL, UriKind.Absolute);
			_authClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(CatCore)}/1.0.0");
		}

		public async Task<AuthorizationResponse?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl)
		{
			var responseMessage = await _authClient
				.PostAsync($"{TWITCH_AUTH_BASEURL}token" +
				           $"?client_id={Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId")}" +
				           $"&client_secret={Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret")}" +
				           $"&code={authorizationCode}" +
				           "&grant_type=authorization_code" +
				           $"&redirect_uri={redirectUrl}", null!)
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				return null;
			}

			return await responseMessage.Content.ReadFromJsonAsync<AuthorizationResponse?>().ConfigureAwait(false);
		}

		public async Task<AuthorizationResponse?> RefreshTokens(string refreshToken)
		{
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				return null;
			}

			var responseMessage = await _authClient
				.PostAsync($"{TWITCH_AUTH_BASEURL}token" +
				           $"?client_id={Environment.GetEnvironmentVariable("Twitch_CatCore_ClientId")}" +
				           $"&client_secret={Environment.GetEnvironmentVariable("Twitch_CatCore_ClientSecret")}" +
				           "&grant_type=refresh_token" +
				           $"&refresh_token={refreshToken}", null!)
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				return null;
			}

			return await responseMessage.Content.ReadFromJsonAsync<AuthorizationResponse?>().ConfigureAwait(false);
		}
	}
}