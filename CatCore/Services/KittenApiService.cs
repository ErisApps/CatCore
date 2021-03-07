using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal class KittenApiService : IKittenApiService
	{
		private readonly ILogger _logger;
		private readonly IKittenSettingsService _settingsService;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly Version _libraryVersion;

		private HttpListener? _listener;
		private string? _webSitePage;

		public KittenApiService(ILogger logger, IKittenSettingsService settingsService, ITwitchAuthService twitchAuthService, Version libraryVersion)
		{
			_logger = logger;
			_settingsService = settingsService;
			_twitchAuthService = twitchAuthService;
			_libraryVersion = libraryVersion;

			logger.Information("Nyaa~~");
		}

		public async Task Initialize()
		{
			if (_webSitePage == null)
			{
				using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(CatCore)}.Resources.index.html")!);
				var pageBuilder = new StringBuilder(await reader.ReadToEndAsync().ConfigureAwait(false));
				pageBuilder.Replace("{libVersion}", _libraryVersion.ToString(3));
				_webSitePage = pageBuilder.ToString();
			}

			_listener = new HttpListener {Prefixes = {$"http://localhost:{8338}/"}};
			_listener.Start();

			await Task.Run(async () =>
			{
				while (true)
				{
					try
					{
						var context = await _listener.GetContextAsync().ConfigureAwait(false);
						await HandleContext(context).ConfigureAwait(false);
					}
					catch (Exception e)
					{
						_logger.Error(e, "An error occured while trying to handle an incoming request");
					}
				}

				// ReSharper disable once FunctionNeverReturns
			}).ConfigureAwait(false);
		}

		private async Task HandleContext(HttpListenerContext ctx)
		{
			var request = ctx.Request;
			using var response = ctx.Response;
			try
			{
				var requestHandled = false;

				if (request.Url.AbsolutePath.StartsWith("/api"))
				{
					requestHandled = await HandleApiRequest(request, response).ConfigureAwait(false);
				}
				else if (request.Url.AbsolutePath.StartsWith("/") && request.HttpMethod == "GET")
				{
					var data = Encoding.UTF8.GetBytes(_webSitePage!);
					response.ContentEncoding = Encoding.UTF8;
					response.ContentLength64 = data.Length;
					response.ContentType = "text/html";
					await response.OutputStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);

					requestHandled = true;
				}

				if (!requestHandled)
				{
					_logger.Warning("{Method} {RequestUrl} went unhandled", request.HttpMethod, request.Url.AbsoluteUri);

					response.StatusCode = 404;
				}
			}
			catch (Exception e)
			{
				_logger.Error(e, "Something went wrong while trying to handle an incoming request");
				response.StatusCode = 500;
			}
		}

		private Task<bool> HandleApiRequest(HttpListenerRequest request, HttpListenerResponse response)
		{
			switch (request.Url.Segments.ElementAtOrDefault(2))
			{
				case "twitch/":
					return HandleTwitchApiRequests(request, response);
				default:
					return Task.FromResult(false);
			}
		}

		private async Task<bool> HandleTwitchApiRequests(HttpListenerRequest request, HttpListenerResponse response)
		{
			switch (request.Url.Segments.ElementAtOrDefault(3))
			{
				case "login" when request.HttpMethod == "GET":
					response.Redirect(_twitchAuthService.AuthorizationUrl("http://localhost:8338/api/twitch/authcode_callback"));
					return true;
				case "authcode_callback" when request.HttpMethod == "GET":
					string? code = null;
					foreach (var parameterPair in request.Url.Query.Substring(1).Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries))
					{
						var kvp = parameterPair.Split('=');
						if (kvp[0] == "code")
						{
							code = kvp[1];
						}
					}

					if (code != null)
					{
						await _twitchAuthService.GetTokensByAuthorizationCode(code, request.Url.GetLeftPart(UriPartial.Path)).ConfigureAwait(false);
					}

					response.Redirect(request.Url.GetLeftPart(UriPartial.Authority));

					return true;
				default:
					return false;
			}
		}
	}
}