using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Helpers.JSON;
using CatCore.Models.Credentials;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Polly;
using Polly.Retry;
using Serilog;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchAuthService : KittenCredentialsProvider<TwitchCredentials>, ITwitchAuthService
	{
		private const string SERVICE_TYPE = nameof(Twitch);
		private const string TWITCH_AUTH_BASEURL = "https://id.twitch.tv/oauth2/";

		private readonly AsyncRetryPolicy<HttpResponseMessage> _exceptionRetryPolicy;

		private readonly SemaphoreSlim _refreshLocker = new(1, 1);
		private readonly SemaphoreSlim _loggedInUserUpdateLocker = new(1, 1);

		private readonly string[] _twitchAuthorizationScope =
		{
			"bits:read",
			"chat:edit",
			"chat:read",
			"channel:manage:broadcast",
			"channel:manage:polls",
			"channel:manage:predictions",
			"channel:manage:raids",
			"channel:manage:redemptions",
			"channel:moderate",
			"channel:read:subscriptions",
			"moderator:manage:announcements",
			"moderator:manage:banned_users",
			"moderator:manage:chat_messages",
			"moderator:manage:chat_settings",
			"user:manage:chat_color",
			"user:read:follows"
		};

		private readonly ILogger _logger;
		private readonly ConstantsBase _constants;
		private readonly HttpClient _twitchAuthClient;
		private readonly HttpClient _catCoreAuthClient;

		private ValidationResponse? _loggedInUser;
		private AuthenticationStatus _status;

		protected override string ServiceType => SERVICE_TYPE;

		public string? AccessToken => Credentials.AccessToken;

		public bool HasTokens => !string.IsNullOrWhiteSpace(AccessToken) && !string.IsNullOrWhiteSpace(Credentials.RefreshToken);

		/// <remark>
		/// Consider token as not valid anymore when it has less than 5 minutes remaining
		/// </remark>
		public bool TokenIsValid => Credentials.ValidUntil > DateTimeOffset.Now.AddMinutes(5);

		public AuthenticationStatus Status
		{
			get => _status;
			private set
			{
				if (_status == value)
				{
					return;
				}

				_status = value;
				OnAuthenticationStatusChanged?.Invoke(_status);
			}
		}

		public event Action<AuthenticationStatus>? OnAuthenticationStatusChanged;

		public TwitchAuthService(ILogger logger, IKittenPathProvider kittenPathProvider, ConstantsBase constants, Version libraryVersion) : base(logger, kittenPathProvider)
		{
			_logger = logger;
			_constants = constants;

			var userAgent = $"{nameof(CatCore)}/{libraryVersion.ToString(3)}";

			_twitchAuthClient = new HttpClient
#if !RELEASE
				(new HttpClientHandler { Proxy = SharedProxyProvider.PROXY })
#endif
				{
					BaseAddress = new Uri(TWITCH_AUTH_BASEURL, UriKind.Absolute)
				};
			_twitchAuthClient.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);

			_catCoreAuthClient = new HttpClient
#if !RELEASE
				(new HttpClientHandler { Proxy = SharedProxyProvider.PROXY })
#endif
				{
					BaseAddress = new Uri(constants.CatCoreAuthServerUri, UriKind.Absolute)
				};
			_catCoreAuthClient.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);

			_exceptionRetryPolicy = Policy<HttpResponseMessage>
				.Handle<HttpRequestException>()
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(2 ^ (retryAttempt - 1) * 500));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ValidationResponse? FetchLoggedInUserInfo()
		{
			return _loggedInUser;
		}

		// ReSharper disable once CognitiveComplexity
		public async Task<ValidationResponse?> FetchLoggedInUserInfoWithRefresh()
		{
			if (HasTokens)
			{
				if (TokenIsValid && _loggedInUser != null)
				{
					return _loggedInUser;
				}

				using var _ = await Synchronization.LockAsync(_loggedInUserUpdateLocker);

				if (TokenIsValid && _loggedInUser != null)
				{
					return _loggedInUser;
				}

				if (Status == AuthenticationStatus.Unauthorized)
				{
					Status = AuthenticationStatus.Initializing;
				}

				try
				{
					var validateAccessToken = await ValidateAccessToken(Credentials, false).ConfigureAwait(false);
					_logger.Information("Validated token: Is valid: {IsValid}, Is refreshable: {IsRefreshable}", validateAccessToken != null && TokenIsValid, Credentials.RefreshToken != null);
					if (validateAccessToken == null || !TokenIsValid)
					{
						_logger.Information("Refreshing tokens");
						await RefreshTokens().ConfigureAwait(false);
					}

					return _loggedInUser;
				}
				catch (HttpRequestException ex)
				{
					_logger.Error(ex, "An error occurred while trying to validate/refresh the Twitch tokens. Make sure an active internet connection is available");
				}
			}
			else
			{
				_logger.Warning("No Twitch Credentials present");
			}

			return null;
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

		public async Task GetTokensByAuthorizationCode(string authorizationCode, string redirectUrl)
		{
			_logger.Information("Exchanging authorization code for credentials using secure CatCore auth back-end");

			try
			{
				using var responseMessage = await _catCoreAuthClient
					.PostAsync($"{_constants.CatCoreAuthServerUri}api/twitch/authorize?code={authorizationCode}&redirect_uri={redirectUrl}", null)
					.ConfigureAwait(false);

				if (!responseMessage.IsSuccessStatusCode)
				{
					return;
				}

				var authorizationResponse = await responseMessage.Content.ReadFromJsonAsync(TwitchAuthSerializerContext.Default.AuthorizationResponse).ConfigureAwait(false);

				var newCredentials = new TwitchCredentials(authorizationResponse);
				await ValidateAccessToken(newCredentials).ConfigureAwait(false);
			}
			catch (HttpRequestException ex)
			{
				_logger.Error(ex, "An error occured while trying to exchange the authorization code for credentials");
			}
			catch (JsonException ex)
			{
				_logger.Error(ex, "An error occured while trying to deserialize the credentials response");
			}
		}

		public async Task<ValidationResponse?> ValidateAccessToken(TwitchCredentials credentials, bool resetDataOnFailure = true)
		{
			if (string.IsNullOrWhiteSpace(credentials.AccessToken))
			{
				return null;
			}

			using var responseMessage = await _exceptionRetryPolicy
				.ExecuteAsync(async () =>
				{
					using var requestMessage = new HttpRequestMessage(HttpMethod.Get, TWITCH_AUTH_BASEURL + "validate");
					requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);
					return await _twitchAuthClient.SendAsync(requestMessage).ConfigureAwait(false);
				})
				.ConfigureAwait(false);

			if (!responseMessage.IsSuccessStatusCode)
			{
				if (resetDataOnFailure)
				{
					UpdateCredentials(TwitchCredentials.Empty());
					_loggedInUser = null;
				}

				Status = AuthenticationStatus.Unauthorized;

				return null;
			}

			var validationResponse = await responseMessage.Content.ReadFromJsonAsync(TwitchAuthSerializerContext.Default.ValidationResponse).ConfigureAwait(false);
			_loggedInUser = validationResponse;

			UpdateCredentials(credentials.ValidUntil!.Value > validationResponse.ExpiresIn
				? new TwitchCredentials(credentials.AccessToken, credentials.RefreshToken, validationResponse.ExpiresIn)
				: credentials);

			Status = AuthenticationStatus.Authenticated;

			return _loggedInUser;
		}

		public async Task<bool> RefreshTokens()
		{
			using var _ = await Synchronization.LockAsync(_refreshLocker);
			if (string.IsNullOrWhiteSpace(Credentials.RefreshToken))
			{
				return false;
			}

			if (TokenIsValid)
			{
				return true;
			}

			_logger.Information("Refreshing tokens using secure CatCore auth back-end");
			try
			{
				using var responseMessage = await _exceptionRetryPolicy.ExecuteAsync(() => _catCoreAuthClient
						.PostAsync($"{_constants.CatCoreAuthServerUri}api/twitch/refresh?refresh_token={Credentials.RefreshToken}", null))
					.ConfigureAwait(false);

				if (!responseMessage.IsSuccessStatusCode)
				{
					_logger.Warning("Refreshing tokens resulted in non-success status code");
					return false;
				}

				var authorizationResponse = await responseMessage.Content.ReadFromJsonAsync(TwitchAuthSerializerContext.Default.AuthorizationResponse).ConfigureAwait(false);

				var refreshedCredentials = new TwitchCredentials(authorizationResponse);
				return await ValidateAccessToken(refreshedCredentials).ConfigureAwait(false) != null;
			}
			catch (JsonException ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to deserialize the refresh tokens body");

				UpdateCredentials(TwitchCredentials.Empty());
				_loggedInUser = null;

				return false;
			}
		}

		public async Task<bool> RevokeTokens()
		{
			if (string.IsNullOrWhiteSpace(Credentials.RefreshToken))
			{
				return false;
			}

			try
			{
				using var responseMessage = await _exceptionRetryPolicy
					.ExecuteAsync(() => _twitchAuthClient.PostAsync($"{TWITCH_AUTH_BASEURL}revoke?client_id={_constants.TwitchClientId}&token={Credentials.RefreshToken}", null))
					.ConfigureAwait(false);

				UpdateCredentials(TwitchCredentials.Empty());
				_loggedInUser = null;

				return responseMessage.IsSuccessStatusCode;
			}
			catch (JsonException ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to deserialize the revoke tokens body");

				return false;
			}
		}
	}
}