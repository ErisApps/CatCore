using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Exceptions;
using CatCore.Helpers;
using CatCore.Models.Twitch.OAuth;
using CatCore.Services.Twitch.Interfaces;
using Polly;
using Polly.Wrap;
using Serilog;

namespace CatCore.Services.Twitch
{
	public sealed partial class TwitchHelixApiService
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

			_helixClient = new HttpClient(new TwitchHelixClientHandler(twitchAuthService)) { BaseAddress = new Uri(TWITCH_HELIX_BASEURL, UriKind.Absolute) };
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

			var exceptionRetryPolicy = Policy<HttpResponseMessage>
				.Handle<HttpRequestException>()
				.OrResult(resp => resp.StatusCode == HttpStatusCode.Conflict)
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(2 ^ (retryAttempt - 1) * 500));

			var bulkheadPolicy = Policy.BulkheadAsync<HttpResponseMessage>(4, 1000);

			_combinedHelixPolicy = Policy.WrapAsync(bulkheadPolicy, enhanceYourCalmPolicy, reAuthPolicy, exceptionRetryPolicy);
		}

		private async Task<ValidationResponse> CheckUserLoggedIn()
		{
			var loggedInUser = await _twitchAuthService.FetchLoggedInUserInfoWithRefresh().ConfigureAwait(false);
			return loggedInUser ?? throw new TwitchNotAuthenticatedException();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task<TResponse?> GetAsync<TResponse>(string url, JsonTypeInfo<TResponse> jsonResponseTypeInfo, CancellationToken cancellationToken = default) where TResponse : struct
			=> CallEndpointNoBodyExpectBody(HttpMethod.Get, url, jsonResponseTypeInfo, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task<TResponse?> PostAsync<TResponse, TBody>(string url, TBody body, JsonTypeInfo<TResponse> jsonResponseTypeInfo, CancellationToken cancellationToken = default)
			where TResponse : struct => CallEndpointWithBodyExpectBody(HttpMethod.Post, url, body, jsonResponseTypeInfo, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task<TResponse?> PostAsync<TResponse>(string url, JsonTypeInfo<TResponse> jsonResponseTypeInfo, CancellationToken cancellationToken = default)
			where TResponse : struct => CallEndpointNoBodyExpectBody(HttpMethod.Post, url, jsonResponseTypeInfo, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task<bool> PostAsync<TBody>(string url, TBody body, CancellationToken cancellationToken = default) => CallEndpointWithBodyNoBody(HttpMethod.Post, url, body, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task<bool> PutAsync(string url, CancellationToken cancellationToken = default) => CallEndpointNoBodyNoBody(HttpMethod.Put, url, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task<TResponse?> PatchAsync<TResponse, TBody>(string url, TBody body, JsonTypeInfo<TResponse> jsonResponseTypeInfo, CancellationToken cancellationToken = default)
			where TResponse : struct => CallEndpointWithBodyExpectBody(HttpMethodPatch, url, body, jsonResponseTypeInfo, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task<bool> PatchAsync<TBody>(string url, TBody body, CancellationToken cancellationToken = default) => CallEndpointWithBodyNoBody(HttpMethodPatch, url, body, cancellationToken);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Task<bool> DeleteAsync(string url, CancellationToken cancellationToken = default) => CallEndpointNoBodyNoBody(HttpMethod.Delete, url, cancellationToken);

		private async Task<TResponse?> CallEndpointNoBodyExpectBody<TResponse>(HttpMethod httpMethod, string url, JsonTypeInfo<TResponse> jsonResponseTypeInfo, CancellationToken cancellationToken = default)
			where TResponse : struct
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentNullException(nameof(url));
			}

			_logger.Verbose("Invoking Helix endpoint {HttpVerb} {Url}", httpMethod, url);
#endif
			if (!_twitchAuthService.HasTokens)
			{
				_logger.Warning("Token not valid. Either the user is not logged in or the token has been revoked");
				return default;
			}

			if (!_twitchAuthService.TokenIsValid && !await _twitchAuthService.RefreshTokens().ConfigureAwait(false))
			{
				return default;
			}

			try
			{
				using var httpResponseMessage = await _combinedHelixPolicy
					.ExecuteAsync(async ct =>
					{
						using var httpRequestMessage = new HttpRequestMessage(httpMethod, url);
						return await _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
					}, cancellationToken).ConfigureAwait(false);
				if (httpResponseMessage == null)
				{
					return null;
				}

				if (!httpResponseMessage.IsSuccessStatusCode)
				{
#if DEBUG
					var errorContent = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
					_logger.Warning("Something went wrong while trying to execute the GET call to {Uri}. StatusCode: {StatusCode}. Response: {Response}",
						url,
						httpResponseMessage.StatusCode,
						errorContent);
#endif
					return null;
				}

				return await httpResponseMessage.Content.ReadFromJsonAsync(jsonResponseTypeInfo, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to execute the GET call to {Uri}", url);
				return null;
			}
		}

		private async Task<bool> CallEndpointNoBodyNoBody(HttpMethod httpMethod, string url, CancellationToken cancellationToken = default)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentNullException(nameof(url));
			}

			_logger.Verbose("Invoking Helix endpoint {HttpVerb} {Url}", httpMethod, url);
#endif
			if (!_twitchAuthService.HasTokens)
			{
				_logger.Warning("Token not valid. Either the user is not logged in or the token has been revoked");
				return default;
			}

			if (!_twitchAuthService.TokenIsValid && !await _twitchAuthService.RefreshTokens().ConfigureAwait(false))
			{
				return default;
			}

			try
			{
				using var httpResponseMessage = await _combinedHelixPolicy.ExecuteAsync(async ct =>
				{
					using var httpRequestMessage = new HttpRequestMessage(httpMethod, url);
					return await _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
				}, cancellationToken).ConfigureAwait(false);
				if (httpResponseMessage == null)
				{
					return false;
				}

#if DEBUG
				if (!httpResponseMessage.IsSuccessStatusCode)
				{
					var errorContent = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
					_logger.Warning("Something went wrong while trying to execute the {HttpVerb} call to {Uri}. StatusCode: {StatusCode}. Response: {Response}",
						httpMethod,
						url,
						httpResponseMessage.StatusCode,
						errorContent);
				}
#endif

				return httpResponseMessage.IsSuccessStatusCode;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to execute the {HttpVerb} call to {Uri}", httpMethod, url);
				return false;
			}
		}

		private async Task<TResponse?> CallEndpointWithBodyExpectBody<TResponse, TBody>(HttpMethod httpMethod, string url, TBody body, JsonTypeInfo<TResponse> jsonResponseTypeInfo,
			CancellationToken cancellationToken = default) where TResponse : struct
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

			_logger.Verbose("Invoking Helix endpoint {HttpVerb} {Url}", httpMethod, url);
#endif
			if (!_twitchAuthService.HasTokens)
			{
				_logger.Warning("Token not valid. Either the user is not logged in or the token has been revoked");
				return default;
			}

			if (!_twitchAuthService.TokenIsValid && !await _twitchAuthService.RefreshTokens().ConfigureAwait(false))
			{
				return default;
			}

			try
			{
				using var httpResponseMessage = await _combinedHelixPolicy.ExecuteAsync(async ct =>
				{
					using var jsonContent = JsonContent.Create(body);
					using var httpRequestMessage = new HttpRequestMessage(httpMethod, url) { Content = jsonContent };
					return await _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
				}, cancellationToken).ConfigureAwait(false);
				if (httpResponseMessage == null)
				{
					return null;
				}

				if (!httpResponseMessage.IsSuccessStatusCode)
				{
#if DEBUG
					var errorContent = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
					_logger.Warning("Something went wrong while trying to execute the {HttpVerb} call to {Uri}. StatusCode: {StatusCode}. Response: {Response}",
						httpMethod,
						url,
						httpResponseMessage.StatusCode,
						errorContent);
#endif
					return null;
				}

				return await httpResponseMessage.Content.ReadFromJsonAsync(jsonResponseTypeInfo, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to execute the {HttpVerb} call to {Uri}", httpMethod, url);
				return null;
			}
		}

		private async Task<bool> CallEndpointWithBodyNoBody<TBody>(HttpMethod httpMethod, string url, TBody body, CancellationToken cancellationToken = default)
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

			_logger.Verbose("Invoking Helix endpoint {HttpVerb} {Url}", httpMethod, url);
#endif
			if (!_twitchAuthService.HasTokens)
			{
				_logger.Warning("Token not valid. Either the user is not logged in or the token has been revoked");
				return default;
			}

			if (!_twitchAuthService.TokenIsValid && !await _twitchAuthService.RefreshTokens().ConfigureAwait(false))
			{
				return default;
			}

			try
			{
				using var httpResponseMessage = await _combinedHelixPolicy.ExecuteAsync(async ct =>
				{
					using var jsonContent = JsonContent.Create(body);
					using var httpRequestMessage = new HttpRequestMessage(httpMethod, url) { Content = jsonContent };
					return await _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
				}, cancellationToken).ConfigureAwait(false);
				if (httpResponseMessage == null)
				{
					return false;
				}

#if DEBUG
				if (!httpResponseMessage.IsSuccessStatusCode)
				{
					var errorContent = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
					_logger.Warning("Something went wrong while trying to execute the {HttpVerb} call to {Uri}. StatusCode: {StatusCode}. Response: {Response}",
						httpMethod,
						url,
						httpResponseMessage.StatusCode,
						errorContent);
				}
#endif

				return httpResponseMessage.IsSuccessStatusCode;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to execute the {HttpVerb} call to {Uri}", httpMethod, url);
				return false;
			}
		}
	}
}