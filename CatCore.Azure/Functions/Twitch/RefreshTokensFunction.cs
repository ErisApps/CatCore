using System.Net;
using System.Threading.Tasks;
using System.Web;
using CatCore.Azure.Services.Twitch;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CatCore.Azure.Functions.Twitch
{
	public class RefreshTokensFunction
	{
		[Function("RefreshTokensFunction")]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "twitch/refresh")]
			HttpRequestData req,
			FunctionContext executionContext)
		{
			var logger = executionContext.GetLogger(nameof(RefreshTokensFunction));

			var queryParametersCollection = HttpUtility.ParseQueryString(req.Url.Query);
			var refreshToken = queryParametersCollection["refresh_token"];
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				logger.LogWarning("API Call is missing one of the required query parameters");
				return req.CreateResponse(HttpStatusCode.BadRequest);
			}

			var twitchAuthService = executionContext.InstanceServices.GetService<TwitchAuthService>()!;
			await using var authorizationResponseStream = await twitchAuthService.RefreshTokens(refreshToken).ConfigureAwait(false);

			HttpResponseData response;
			if (authorizationResponseStream != null)
			{
				response = req.CreateResponse(HttpStatusCode.OK);
				await authorizationResponseStream.CopyToAsync(response.Body).ConfigureAwait(false);
			}
			else
			{
				logger.LogInformation("Couldn't refresh existing credentials through Twitch Auth server");
				response = req.CreateResponse(HttpStatusCode.Unauthorized);
			}

			return response;
		}
	}
}