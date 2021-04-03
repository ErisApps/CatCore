using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CatCore.Models.Credentials;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal class TwitchAuthService : KittenCredentialsProvider<TwitchCredentials>, ITwitchAuthService
	{
		private const string SERVICE_TYPE = nameof(Twitch);
		private const string TWITCH_AUTH_BASEURL = "https://id.twitch.tv/oauth2/";

		private readonly string[] _twitchAuthorizationScope =
		{
			"channel:moderate", "chat:edit", "chat:read", "whispers:read", "whispers:edit", "bits:read", "channel:manage:broadcast", "channel:read:redemptions", "channel:read:subscriptions"
		};

		private readonly ILogger _logger;
		private readonly ConstantsBase _constants;
		private readonly HttpClient _authClient;

		protected override string ServiceType => SERVICE_TYPE;

		private DateTimeOffset? ValidUntil
		{
			get => Credentials.ValidUntil;
			set => Credentials.ValidUntil = value;
		}

		private string? RefreshToken
		{
			get => Credentials.RefreshToken;
			set => Credentials.RefreshToken = value;
		}

		public string? AccessToken
		{
			get => Credentials.AccessToken;
			private set => Credentials.AccessToken = value;
		}

		public bool IsValid => !string.IsNullOrWhiteSpace(AccessToken) && !string.IsNullOrWhiteSpace(RefreshToken) && ValidUntil > DateTimeOffset.Now;

		public ValidationResponse? LoggedInUser { get; private set; }

		public TwitchAuthService(ILogger logger, IKittenPathProvider kittenPathProvider, ConstantsBase constants, Version libraryVersion) : base(logger, kittenPathProvider)
		{
			_logger = logger;
			_constants = constants;

			_authClient = new HttpClient {BaseAddress = new Uri(TWITCH_AUTH_BASEURL, UriKind.Absolute)};
			_authClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(CatCore)}/{libraryVersion.ToString(3)}");
		}

		public async Task Initialize()
		{
			_logger.Information("Validating Twitch Credentials");

			if (!string.IsNullOrWhiteSpace(AccessToken) && !string.IsNullOrWhiteSpace(RefreshToken))
			{
				var validateAccessToken = await ValidateAccessToken().ConfigureAwait(false);
				_logger.Information("Validated token: Is valid: {IsValid}, Is refreshable: {IsRefreshable}", AccessToken != null, RefreshToken != null);
				if (validateAccessToken != null && ValidUntil > DateTimeOffset.Now.AddMinutes(5))
				{
					await RefreshTokens().ConfigureAwait(false);
				}
			}
			else
			{
				_logger.Warning("No Twitch Credentials present");
			}
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
			ValidUntil = authorizationResponse.Value.ExpiresIn;

			Store();

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

			using var _ = ChangeTransaction();
			if (!responseMessage.IsSuccessStatusCode)
			{
				AccessToken = null;
				RefreshToken = null;
				ValidUntil = null;

				return null;
			}

			LoggedInUser = await responseMessage.Content.ReadFromJsonAsync<ValidationResponse?>().ConfigureAwait(false);
			ValidUntil = LoggedInUser?.ExpiresIn;

			return LoggedInUser;
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

			using var transaction = ChangeTransaction();
			if (authorizationResponse == null)
			{
				AccessToken = null;
				RefreshToken = null;
				ValidUntil = null;

				return false;
			}

			AccessToken = authorizationResponse.Value.AccessToken;
			RefreshToken = authorizationResponse.Value.RefreshToken;
			ValidUntil = authorizationResponse.Value.ExpiresIn;

			return await ValidateAccessToken().ConfigureAwait(false) != null;
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

			Store();

			return responseMessage.IsSuccessStatusCode;
		}
	}
}