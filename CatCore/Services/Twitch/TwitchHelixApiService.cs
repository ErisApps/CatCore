using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Services.Twitch.Interfaces;
using Polly;
using Polly.Wrap;
using Serilog;

namespace CatCore.Services.Twitch
{
	public partial class TwitchHelixApiService : ITwitchHelixApiService
	{
		private const string TWITCH_HELIX_BASEURL = "https://api.twitch.tv/helix/";

		// I can't believe that I actually have to do this...
		private static readonly HttpMethod HttpMethodPatch = new("PATCH");

		private readonly ILogger _logger;
		private readonly ITwitchAuthService _twitchAuthService;

		private readonly HttpClient _helixClient;
		private readonly AsyncPolicyWrap<HttpResponseMessage> _combinedHelixPolicy;

		internal TwitchHelixApiService(ILogger logger, ITwitchAuthService twitchAuthService, ConstantsBase constants, Version libraryVersion)
		{
			_logger = logger;
			_twitchAuthService = twitchAuthService;

			_helixClient = new HttpClient(new TwitchHelixClientHandler(twitchAuthService)) {BaseAddress = new Uri(TWITCH_HELIX_BASEURL, UriKind.Absolute)};
			_helixClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(CatCore)}/{libraryVersion.ToString(3)}");
			_helixClient.DefaultRequestHeaders.TryAddWithoutValidation("Client-ID", constants.TwitchClientId);

			var reAuthPolicy = Policy
				.HandleResult<HttpResponseMessage>(resp => resp.StatusCode == HttpStatusCode.Unauthorized)
				.RetryAsync(1, async (_, _, _) =>
				{
					_logger.Information("Attempting re-auth");
					await twitchAuthService.RefreshTokens().ConfigureAwait(false);
				});

			var enhanceYourCalmPolicy = Policy
				.HandleResult<HttpResponseMessage>(resp => resp.StatusCode == StatusCodeHelper.TOO_MANY_REQUESTS)
				.WaitAndRetryAsync(
					1,
					(retryAttempt, response, _) =>
					{
						response.Result.Headers.TryGetValues("Ratelimit-Reset", out var values);
						if (values != null && long.TryParse(values.FirstOrDefault(), out var unixMillisTillReset))
						{
							return TimeSpan.FromMilliseconds(unixMillisTillReset - DateTimeOffset.Now.ToUnixTimeMilliseconds());
						}

						return TimeSpan.FromSeconds(Math.Pow(10, retryAttempt));
					},
					((_, timeSpan, _, _) =>
					{
						_logger.Information("Hit Helix rate limit. Retrying in {TimeTillReset}", timeSpan.ToString("g"));
						return Task.CompletedTask;
					}));

			var bulkheadPolicy = Policy.BulkheadAsync<HttpResponseMessage>(1, 1000);

			_combinedHelixPolicy = Policy.WrapAsync(bulkheadPolicy, enhanceYourCalmPolicy, reAuthPolicy);
		}

		private async Task<TResponse?> GetAsync<TResponse>(string url, CancellationToken? cancellationToken = null) where TResponse : struct
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentNullException(nameof(url));
			}

			_logger.Verbose("Invoking Helix endpoint GET {Url}", url);
#endif
			if (!_twitchAuthService.HasTokens)
			{
				_logger.Warning("Token not valid. Either the user is not logged in or the token has been revoked");
				return null;
			}

			if (!_twitchAuthService.TokenIsValid && !await _twitchAuthService.RefreshTokens().ConfigureAwait(false))
			{
				return null;
			}

			using var httpResponseMessage = await _combinedHelixPolicy
				.ExecuteAsync(() => _helixClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken ?? CancellationToken.None)).ConfigureAwait(false);
			if (!(httpResponseMessage?.IsSuccessStatusCode ?? false))
			{
				return null;
			}

			return await httpResponseMessage.Content.ReadFromJsonAsync<TResponse>(options: null, cancellationToken ?? default).ConfigureAwait(false);
		}

		private Task<TResponse?> PostAsync<TResponse, TBody>(string url, TBody body, CancellationToken? cancellationToken = null) where TResponse : struct =>
			CallEndpointWithBodyExpectBody<TResponse, TBody>(HttpMethod.Post, url, body, cancellationToken);

		private Task<bool> PostAsync<TBody>(string url, TBody body, CancellationToken? cancellationToken = null) => CallEndpointWithBodyNoBody(HttpMethod.Post, url, body, cancellationToken);

		private Task<TResponse?> PatchAsync<TResponse, TBody>(string url, TBody body, CancellationToken? cancellationToken = null) where TResponse : struct =>
			CallEndpointWithBodyExpectBody<TResponse, TBody>(HttpMethodPatch, url, body, cancellationToken);

		private Task<bool> PatchAsync<TBody>(string url, TBody body, CancellationToken? cancellationToken = null) => CallEndpointWithBodyNoBody(HttpMethodPatch, url, body, cancellationToken);

		private async Task<TResponse?> CallEndpointWithBodyExpectBody<TResponse, TBody>(HttpMethod httpMethod, string url, TBody body, CancellationToken? cancellationToken = null)
			where TResponse : struct
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentNullException(nameof(url));
			}

			if (body == null)
			{
				throw new ArgumentNullException(nameof(body));
			}

			_logger.Verbose("Invoking Helix endpoint POST {Url}", url);
#endif
			if (!_twitchAuthService.HasTokens)
			{
				_logger.Warning("Token not valid. Either the user is not logged in or the token has been revoked");
				return null;
			}

			if (!_twitchAuthService.TokenIsValid && !await _twitchAuthService.RefreshTokens().ConfigureAwait(false))
			{
				return null;
			}

			using var httpResponseMessage = await _combinedHelixPolicy.ExecuteAsync(() =>
			{
				var jsonContent = JsonContent.Create(body);
				var httpRequestMessage = new HttpRequestMessage(httpMethod, url) {Content = jsonContent};
				return _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken ?? default);
			}).ConfigureAwait(false);
			if (!(httpResponseMessage?.IsSuccessStatusCode ?? false))
			{
				return null;
			}

			return await httpResponseMessage.Content.ReadFromJsonAsync<TResponse>(options: null, cancellationToken ?? default).ConfigureAwait(false);
		}

		private async Task<bool> CallEndpointWithBodyNoBody<TBody>(HttpMethod httpMethod, string url, TBody body, CancellationToken? cancellationToken = null)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentNullException(nameof(url));
			}

			if (body == null)
			{
				throw new ArgumentNullException(nameof(body));
			}

			_logger.Verbose("Invoking Helix endpoint POST {Url}", url);
#endif
			if (!_twitchAuthService.HasTokens)
			{
				_logger.Warning("Token not valid. Either the user is not logged in or the token has been revoked");
				return false;
			}

			if (!_twitchAuthService.TokenIsValid && !await _twitchAuthService.RefreshTokens().ConfigureAwait(false))
			{
				return false;
			}

			using var httpResponseMessage = await _combinedHelixPolicy.ExecuteAsync(() =>
			{
				var jsonContent = JsonContent.Create(body);
				var httpRequestMessage = new HttpRequestMessage(httpMethod, url) {Content = jsonContent};
				return _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken ?? default);
			}).ConfigureAwait(false);
			return httpResponseMessage?.IsSuccessStatusCode ?? false;
		}
	}
}