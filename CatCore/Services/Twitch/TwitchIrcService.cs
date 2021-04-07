using System;
using System.Threading.Tasks;
using CatCore.Services.Twitch.Interfaces;
using Serilog;
using Websocket.Client;

namespace CatCore.Services.Twitch
{
	internal class TwitchIrcService : KittenWebSocketProvider, ITwitchIrcService
	{
		private const string TWITCH_IRC_ENDPOINT = "wss://irc-ws.chat.twitch.tv:443";

		private readonly ILogger _logger;
		private readonly ITwitchAuthService _twitchAuthService;

		public TwitchIrcService(ILogger logger, ITwitchAuthService twitchAuthService) : base(logger)
		{
			_logger = logger;
			_twitchAuthService = twitchAuthService;
		}

		public Task Start()
		{
			return Connect(TWITCH_IRC_ENDPOINT);
		}

		public Task Stop()
		{
			return Disconnect("Requested by service manager");
		}

		private void WebSocketProviderOnOnOpen()
		{
			_logger.Information("Opened connection to Twitch IRC server");

			SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");

			SendMessage($"PASS oauth:{_twitchAuthService.AccessToken}");
			SendMessage($"NICK {_twitchAuthService.LoggedInUser?.LoginName ?? "."}");
		}

		protected override void MessageReceivedHandler(ResponseMessage response)
		{
			var messages = response.Text.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

			foreach (var messageInternal in messages)
			{
				// Handle IRC messages here
			}
		}

		protected override void DisconnectHappenedHandler(DisconnectionInfo info)
		{
			_logger.Information("Closed connection to Twitch IRC server");
		}
	}
}