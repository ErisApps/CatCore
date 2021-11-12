using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Services.Twitch.Interfaces;

namespace CatCore.Services.Twitch
{
	internal sealed class TwitchHelixClientHandler : HttpClientHandler
	{
		private readonly ITwitchAuthService _twitchAuthService;

		public TwitchHelixClientHandler(ITwitchAuthService twitchAuthService)
		{
			_twitchAuthService = twitchAuthService;

#if !RELEASE
			Proxy = Helpers.SharedProxyProvider.PROXY;
#endif
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _twitchAuthService.AccessToken);

			return base.SendAsync(request, cancellationToken);
		}
	}
}