using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal class TwitchAuthService : ITwitchAuthService
	{
		private const string TWITCH_AUTH_BASEURL = "https://id.twitch.tv/oauth2/";

		private const string TWITCH_CLIENT_ID = "qe9e3dxhs2i17n8d747wwiet8x671w";
		private const string TWITCH_CLIENT_SECRET = "mflv88r48wrfheyjpmq8osir4obpou";

		private readonly string[] _twitchAuthorizationScope = {"channel:moderate", "chat:edit", "chat:read", "whispers:read", "whispers:edit", "bits:read", "channel:read:redemptions"};

		private readonly ILogger _logger;
		private readonly HttpClient _authClient;

		private string? _accessToken;
		private string? _refreshToken;

		public TwitchAuthService(ILogger logger, HttpClient authClient)
		{
			_logger = logger;
			_authClient = authClient;

			_authClient.BaseAddress = new Uri(TWITCH_AUTH_BASEURL, UriKind.Absolute);
		}

		public string AuthorizationUrl(string redirectUrl)
		{
			return $"{TWITCH_AUTH_BASEURL}authorize" +
			       $"?client_id={TWITCH_CLIENT_ID}" +
			       $"&redirect_uri={redirectUrl}" +
			       "&response_type=code" +
			       "&force_verify=true" +
			       $"&scope={string.Join(" ", _twitchAuthorizationScope)}";
		}

		public async Task<AuthorizationResponse?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl)
		{
			var responseMessage = await _authClient
				.PostAsync("https://id.twitch.tv/oauth2/token" +
				           $"?client_id={TWITCH_CLIENT_ID}" +
				           $"&client_secret={TWITCH_CLIENT_SECRET}" +
				           $"&code={authorizationCode}" +
				           "&grant_type=authorization_code" +
				           $"&redirect_uri={redirectUrl}", null)
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				return null;
			}

			var authorizationResponse = await JsonSerializer.DeserializeAsync<AuthorizationResponse?>(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
			if (authorizationResponse == null)
			{
				return null;
			}

			_accessToken = authorizationResponse.Value.AccessToken;
			_refreshToken = authorizationResponse.Value.RefreshToken;

			return authorizationResponse;
		}

		public async Task<ValidationResponse?> ValidateAccessToken()
		{
			if (string.IsNullOrWhiteSpace(_accessToken))
			{
				return null;
			}

			using var requestMessage = new HttpRequestMessage(HttpMethod.Get, TWITCH_AUTH_BASEURL + "validate");
			requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
			var responseMessage = await _authClient.SendAsync(requestMessage).ConfigureAwait(false);

			if (responseMessage.IsSuccessStatusCode)
			{
				return await JsonSerializer.DeserializeAsync<ValidationResponse?>(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
			}

			return null;
		}

		public async Task<bool> RefreshTokens()
		{
			if (string.IsNullOrWhiteSpace(_refreshToken))
			{
				return false;
			}

			var responseMessage = await _authClient
				.PostAsync("https://id.twitch.tv/oauth2/token" +
				           $"?client_id={TWITCH_CLIENT_ID}" +
				           $"&client_secret={TWITCH_CLIENT_SECRET}" +
				           "&grant_type=refresh_token" +
				           $"&refresh_token={_refreshToken}", null)
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				return false;
			}

			var authorizationResponse = await JsonSerializer.DeserializeAsync<AuthorizationResponse?>(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
			if (authorizationResponse == null)
			{
				return false;
			}

			_accessToken = authorizationResponse.Value.AccessToken;
			_refreshToken = authorizationResponse.Value.RefreshToken;

			return true;
		}

		public async Task<bool> RevokeTokens()
		{
			if (string.IsNullOrWhiteSpace(_accessToken))
			{
				return false;
			}

			var responseMessage = await _authClient.PostAsync($"{TWITCH_AUTH_BASEURL}revoke?client_id={TWITCH_CLIENT_ID}&token={_refreshToken}", null);

			_accessToken = null;
			_refreshToken = null;

			return responseMessage.IsSuccessStatusCode;
		}
	}
}