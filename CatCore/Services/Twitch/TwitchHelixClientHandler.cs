using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Services.Twitch.Interfaces;

namespace CatCore.Services.Twitch
{
	internal class TwitchHelixClientHandler : HttpClientHandler
	{
		private readonly ITwitchCredentialsProvider _twitchAuthService;

		public TwitchHelixClientHandler(ITwitchCredentialsProvider twitchAuthService)
		{
			_twitchAuthService = twitchAuthService;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _twitchAuthService.Credentials.AccessToken);

			return base.SendAsync(request, cancellationToken);
		}
	}
}