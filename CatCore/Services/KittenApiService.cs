using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CatCore.Services.Interfaces;
using Serilog;

namespace CatCore.Services
{
	internal class KittenApiService : IKittenApiService
	{
		private readonly ILogger _logger;
		private readonly IKittenSettingsService _settingsService;
		private readonly Version _libraryVersion;

		private HttpListener? _listener;
		private string? _webSitepage;

		public KittenApiService(ILogger logger, IKittenSettingsService settingsService, Version libraryVersion)
		{
			_logger = logger;
			_settingsService = settingsService;
			_libraryVersion = libraryVersion;

			logger.Information("Nyaa~~");
		}

		public async Task Initialize()
		{
			if (_webSitepage == null)
			{
				using var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(CatCore)}.Resources.index.html")!);
				var pageBuilder = new StringBuilder(await reader.ReadToEndAsync().ConfigureAwait(false));
				pageBuilder.Replace("{libVersion}", _libraryVersion.ToString(3));
				_webSitepage = pageBuilder.ToString();
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
					var data = Encoding.UTF8.GetBytes(_webSitepage!);
					response.ContentEncoding = Encoding.UTF8;
					response.ContentLength64 = data.Length;
					response.ContentType = "text/html";
					await response.OutputStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);

					requestHandled = true;
				}

				if (!requestHandled)
				{
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
				default:
					return Task.FromResult(false);
			}
		}
	}
}