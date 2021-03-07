using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal class TwitchAuthService : ITwitchAuthService
	{
		private const string TWITCH_AUTH_BASEURL = "https://id.twitch.tv/oauth2/";

		private readonly string[] _twitchAuthorizationScope =
		{
			"channel:moderate", "chat:edit", "chat:read", "whispers:read", "whispers:edit", "bits:read", "channel:manage:broadcast", "channel:read:redemptions", "channel:read:subscriptions"
		};

		private readonly ILogger _logger;
		private readonly ITwitchCredentialsProvider _credentialsProvider;
		private readonly ConstantsBase _constants;
		private readonly HttpClient _authClient;

		private string? AccessToken
		{
			get => _credentialsProvider.Credentials.AccessToken;
			set => _credentialsProvider.Credentials.AccessToken = value;
		}

		private string? RefreshToken
		{
			get => _credentialsProvider.Credentials.RefreshToken;
			set => _credentialsProvider.Credentials.RefreshToken = value;
		}

		private DateTimeOffset? ValidUntil
		{
			get => _credentialsProvider.Credentials.ValidUntil;
			set => _credentialsProvider.Credentials.ValidUntil = value;
		}

		public TwitchAuthService(ILogger logger, ITwitchCredentialsProvider credentialsProvider, ConstantsBase constants, Version libraryVersion)
		{
			_logger = logger;
			_credentialsProvider = credentialsProvider;
			_constants = constants;

			_authClient = new HttpClient {BaseAddress = new Uri(TWITCH_AUTH_BASEURL, UriKind.Absolute)};
			_authClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(CatCore)}/{libraryVersion.ToString(3)}");
		}

		public string AuthorizationUrl(string redirectUrl)
		{
			return $"{TWITCH_AUTH_BASEURL}authorize" +
			       $"?client_id={_constants.TwitchClientId}" +
			       $"&redirect_uri={redirectUrl}" +
			       "&response_type=code" +
			       "&force_verify=true" +
			       $"&scope={string.Join(" ", _twitchAuthorizationScope)}";
		}

		public async Task<AuthorizationResponse?> GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl)
		{
			var responseMessage = await _authClient
				.PostAsync("https://id.twitch.tv/oauth2/token" +
				           $"?client_id={_constants.TwitchClientId}" +
				           $"&client_secret={_constants.TwitchClientSecret}" +
				           $"&code={authorizationCode}" +
				           "&grant_type=authorization_code" +
				           $"&redirect_uri={redirectUrl}", null)
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				return null;
			}

			var authorizationResponse = await responseMessage.Content.ReadFromJsonAsync<AuthorizationResponse?>().ConfigureAwait(false);
			if (authorizationResponse == null)
			{
				return null;
			}

			AccessToken = authorizationResponse.Value.AccessToken;
			RefreshToken = authorizationResponse.Value.RefreshToken;
			ValidUntil = DateTimeOffset.Now.AddSeconds(authorizationResponse.Value.ExpiresIn);

			return authorizationResponse;
		}

		public async Task<ValidationResponse?> ValidateAccessToken()
		{
			if (string.IsNullOrWhiteSpace(AccessToken))
			{
				return null;
			}

			using var requestMessage = new HttpRequestMessage(HttpMethod.Get, TWITCH_AUTH_BASEURL + "validate");
			requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
			var responseMessage = await _authClient.SendAsync(requestMessage).ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				AccessToken = null;
				RefreshToken = null;
				ValidUntil = null;

				_credentialsProvider.Store();

				return null;
			}

			return await responseMessage.Content.ReadFromJsonAsync<ValidationResponse?>().ConfigureAwait(false);
		}

		public async Task<bool> RefreshTokens()
		{
			if (string.IsNullOrWhiteSpace(RefreshToken))
			{
				return false;
			}

			var responseMessage = await _authClient
				.PostAsync("https://id.twitch.tv/oauth2/token" +
				           $"?client_id={_constants.TwitchClientId}" +
				           $"&client_secret={_constants.TwitchClientSecret}" +
				           "&grant_type=refresh_token" +
				           $"&refresh_token={RefreshToken}", null)
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				return false;
			}

			var authorizationResponse = await responseMessage.Content.ReadFromJsonAsync<AuthorizationResponse?>().ConfigureAwait(false);

			using var transaction = _credentialsProvider.ChangeTransaction();
			if (authorizationResponse == null)
			{
				AccessToken = null;
				RefreshToken = null;
				ValidUntil = null;

				return false;
			}

			AccessToken = authorizationResponse.Value.AccessToken;
			RefreshToken = authorizationResponse.Value.RefreshToken;
			ValidUntil = DateTimeOffset.Now.AddSeconds(authorizationResponse.Value.ExpiresIn);

			return true;
		}

		public async Task<bool> RevokeTokens()
		{
			if (string.IsNullOrWhiteSpace(AccessToken))
			{
				return false;
			}

			var responseMessage = await _authClient.PostAsync($"{TWITCH_AUTH_BASEURL}revoke?client_id={_constants.TwitchClientId}&token={RefreshToken}", null);

			AccessToken = null;
			RefreshToken = null;
			ValidUntil = null;

			_credentialsProvider.Store();

			return responseMessage.IsSuccessStatusCode;
		}
	}
}