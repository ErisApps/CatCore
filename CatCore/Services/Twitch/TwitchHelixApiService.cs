using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers;
using CatCore.Models.Twitch.Helix.Requests;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Services.Twitch.Interfaces;
using Polly;
using Polly.Wrap;
using Serilog;

namespace CatCore.Services.Twitch
{
	public class TwitchHelixApiService : ITwitchHelixApiService
	{
		private const string TWITCH_HELIX_BASEURL = "https://api.twitch.tv/helix/";

		private readonly ILogger _logger;

		private readonly HttpClient _helixClient;
		private readonly AsyncPolicyWrap<HttpResponseMessage> _combinedHelixPolicy;

		internal TwitchHelixApiService(ILogger logger, ITwitchCredentialsProvider credentialsProvider, ITwitchAuthService twitchAuthService, ConstantsBase constants, Version libraryVersion)
		{
			_logger = logger;

			_helixClient = new HttpClient(new TwitchHelixClientHandler(credentialsProvider)) {BaseAddress = new Uri(TWITCH_HELIX_BASEURL, UriKind.Absolute)};
			_helixClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(CatCore)}/{libraryVersion.ToString(3)}");
			_helixClient.DefaultRequestHeaders.TryAddWithoutValidation("Client-ID", constants.TwitchClientId);

			var reAuthPolicy = Policy
				.HandleResult<HttpResponseMessage>(resp => resp.StatusCode == HttpStatusCode.Unauthorized)
				.RetryAsync(1, async (response, retryAttempt, context) =>
				{
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
					((_, timeSpan, __, ___) =>
					{
						_logger.Information("Hit Helix rate limit. Retrying in {TimeTillReset}", timeSpan.ToString("g"));
						return Task.CompletedTask;
					}));

			var bulkheadPolicy = Policy.BulkheadAsync<HttpResponseMessage>(1, 1000);

			_combinedHelixPolicy = Policy.WrapAsync(bulkheadPolicy, enhanceYourCalmPolicy, reAuthPolicy);
		}

		public Task<ResponseBase<UserData>?> FetchUserInfo(CancellationToken? cancellationToken = null, params string[] loginNames)
		{
			var uriBuilder = new StringBuilder($"{TWITCH_HELIX_BASEURL}users");
			if (loginNames.Any())
			{
				uriBuilder.Append($"?login={loginNames.First()}");
				for (var i = 1; i < loginNames.Length; i++)
				{
					var loginName = loginNames[i];
					uriBuilder.Append($"&login={loginName}");
				}
			}

			return GetAsyncS<ResponseBase<UserData>>(uriBuilder.ToString(), cancellationToken);
		}

		public Task<ResponseBase<CreateStreamMarkerData>?> CreateStreamMarker(string userId, string? description = null, CancellationToken? cancellationToken = null)
		{
			// add description validation, max 140 chars
			if (!string.IsNullOrWhiteSpace(description) && description!.Length > 140)
			{
				throw new ArgumentException("The description argument is enforced to be 140 characters tops by Helix. Please use a shorter one.", nameof(description));
			}

			var body = new CreateStreamMarkerRequestDto(userId, description);
			return PostAsyncS<ResponseBase<CreateStreamMarkerData>, CreateStreamMarkerRequestDto>($"{TWITCH_HELIX_BASEURL}streams/markers", body, cancellationToken);
		}

		public Task<ResponseBaseWithPagination<ChannelData>?> SearchChannels(string query, uint? limit = null, bool? liveOnly = null, string? continuationCursor = null,
			CancellationToken? cancellationToken = null)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				throw new ArgumentException("The query parameter should not be null, empty or whitespace.", nameof(query));
			}

			var urlBuilder = new StringBuilder($"{TWITCH_HELIX_BASEURL}search/channels?query={query}");
			if (limit != null)
			{
				if (limit.Value > 100)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 100.", nameof(limit));
				}

				urlBuilder.Append($"&first={limit}");
			}

			if (liveOnly != null)
			{
				urlBuilder.Append($"live_only={liveOnly}");
			}

			if (continuationCursor != null)
			{
				if (string.IsNullOrWhiteSpace(query))
				{
					throw new ArgumentException("The continuationCursor parameter should not be null, empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append($"after={continuationCursor}");
			}

			return GetAsyncS<ResponseBaseWithPagination<ChannelData>>(urlBuilder.ToString(), cancellationToken);
		}

		private async Task<TResponse?> GetAsyncS<TResponse>(string url, CancellationToken? cancellationToken = null) where TResponse : struct
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentNullException(nameof(url));
			}
#endif

			using var httpResponseMessage = await _combinedHelixPolicy
				.ExecuteAsync(() => _helixClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken ?? CancellationToken.None)).ConfigureAwait(false);
			if (!(httpResponseMessage?.IsSuccessStatusCode ?? false))
			{
				return null;
			}

			return await httpResponseMessage.Content.ReadFromJsonAsync<TResponse>(null, cancellationToken ?? default).ConfigureAwait(false);
		}

		private async Task<TResponse?> PostAsyncS<TResponse, TBody>(string url, TBody body, CancellationToken? cancellationToken = null) where TResponse : struct
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
#endif

			using var httpResponseMessage = await _combinedHelixPolicy.ExecuteAsync(() =>
			{
				var jsonContent = JsonContent.Create(body);
				var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url) {Content = jsonContent};
				return _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken ?? default);
			}).ConfigureAwait(false);
			if (!(httpResponseMessage?.IsSuccessStatusCode ?? false))
			{
				return null;
			}

			return await httpResponseMessage.Content.ReadFromJsonAsync<TResponse>(null, cancellationToken ?? default).ConfigureAwait(false);
		}
	}
}