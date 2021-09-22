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
	public static class AuthorizationCodeToTokensFunction
	{
		[Function("AuthorizationCodeToTokensFunction")]
		public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "twitch/authorize")]
			HttpRequestData req,
			FunctionContext executionContext)
		{
			var logger = executionContext.GetLogger(nameof(AuthorizationCodeToTokensFunction));

			var queryParametersCollection = HttpUtility.ParseQueryString(req.Url.Query);
			var authorizationCode = queryParametersCollection["code"];
			var redirectUrl = queryParametersCollection["redirect_uri"];
			if (string.IsNullOrWhiteSpace(authorizationCode) || string.IsNullOrWhiteSpace(redirectUrl))
			{
				logger.LogInformation("API Call is missing one of the required query parameters");
				return req.CreateResponse(HttpStatusCode.BadRequest);
			}

			var twitchAuthService = executionContext.InstanceServices.GetService<TwitchAuthService>()!;
			await using var authorizationResponseStream = await twitchAuthService.GetTokensByAuthorizationCode(authorizationCode, redirectUrl).ConfigureAwait(false);

			HttpResponseData response;
			if (authorizationResponseStream != null)
			{
				response = req.CreateResponse(HttpStatusCode.OK);
				await authorizationResponseStream.CopyToAsync(response.Body).ConfigureAwait(false);
			}
			else
			{
				logger.LogInformation("Couldn't trade authorization code for credentials from Twitch Auth server");
				response = req.CreateResponse(HttpStatusCode.Unauthorized);
			}

			return response;
		}
	}
}