using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatCore.Models.Config;
using CatCore.Models.EventArgs;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.Media
{
	internal class TwitchMediaDataProvider : INeedInitialization
	{
		private readonly ILogger _logger;
		private readonly IKittenSettingsService _kittenSettingsService;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;
		private readonly TwitchBadgeDataProvider _twitchBadgeDataProvider;
		private readonly TwitchCheermoteDataProvider _twitchCheermoteDataProvider;
		private readonly BttvDataProvider _bttvDataProvider;

		private bool _cheermotesEnabled;
		private bool _bttvEnabled;
		private bool _ffzEnabled;

		public TwitchMediaDataProvider(ILogger logger, IKittenSettingsService kittenSettingsService, ITwitchAuthService twitchAuthService,
			ITwitchChannelManagementService twitchChannelManagementService, TwitchBadgeDataProvider twitchBadgeDataProvider, TwitchCheermoteDataProvider twitchCheermoteDataProvider,
			BttvDataProvider bttvDataProvider)
		{
			_logger = logger;
			_kittenSettingsService = kittenSettingsService;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;

			_twitchBadgeDataProvider = twitchBadgeDataProvider;
			_twitchCheermoteDataProvider = twitchCheermoteDataProvider;
			_bttvDataProvider = bttvDataProvider;

			_kittenSettingsService.OnConfigChanged += KittenSettingsServiceOnConfigChanged;
			_twitchAuthService.OnCredentialsChanged += TwitchAuthServiceOnCredentialsChanged;
			_twitchChannelManagementService.ChannelsUpdated += TwitchChannelManagementServiceOnChannelsUpdated;
		}

		public void Initialize()
		{
			var initTasks = new List<Task> { TryRequestGlobalResources() };
			initTasks.AddRange(_twitchChannelManagementService.GetAllActiveChannelIds().Select(userId => TryRequestChannelResources(userId)));
			_ = Task.WhenAll(initTasks).ConfigureAwait(false);
		}

		// ReSharper disable once CognitiveComplexity
		private void KittenSettingsServiceOnConfigChanged(IKittenSettingsService settingsService, ConfigRoot config)
		{
			_ = Task.Run(async () =>
			{
				var twitchConfig = config.TwitchConfig;
				var userIds = _twitchChannelManagementService.GetAllActiveChannelIds();
				var initTasks = new List<Task>();

				if (twitchConfig.ParseCheermotes != _cheermotesEnabled)
				{
					_cheermotesEnabled = twitchConfig.ParseCheermotes;

					if (twitchConfig.ParseCheermotes)
					{
						initTasks.Add(_twitchCheermoteDataProvider.TryRequestGlobalResources());
						initTasks.AddRange(userIds.Select(userId => _twitchCheermoteDataProvider.TryRequestChannelResources(userId)));
					}
					else
					{
						_twitchCheermoteDataProvider.ReleaseAllResources();
					}
				}

				if (twitchConfig.ParseBttvEmotes != _bttvEnabled)
				{
					_bttvEnabled = twitchConfig.ParseBttvEmotes;

					if (twitchConfig.ParseBttvEmotes)
					{
						initTasks.Add(_bttvDataProvider.TryRequestGlobalBttvResources());
						initTasks.AddRange(userIds.Select(userId => _bttvDataProvider.TryRequestBttvChannelResources(userId)));
					}
					else
					{
						_bttvDataProvider.ReleaseBttvResources();
					}
				}

				if (twitchConfig.ParseFfzEmotes != _ffzEnabled)
				{
					_ffzEnabled = twitchConfig.ParseFfzEmotes;

					if (twitchConfig.ParseFfzEmotes)
					{
						initTasks.Add(_bttvDataProvider.TryRequestGlobalFfzResources());
						initTasks.AddRange(userIds.Select(userId => _bttvDataProvider.TryRequestFfzChannelResources(userId)));
					}
					else
					{
						_bttvDataProvider.ReleaseFfzResources();
					}
				}

				await Task.WhenAll(initTasks).ConfigureAwait(false);
			});
		}

		private void TwitchAuthServiceOnCredentialsChanged()
		{
			_ = Task.Run(async () =>
			{
				if (!_twitchAuthService.HasTokens && !_twitchAuthService.TokenIsValid)
				{
					return;
				}

				var twitchConfig = _kittenSettingsService.Config.TwitchConfig;
				var userIds = _twitchChannelManagementService.GetAllActiveChannelIds();
				var initTasks = new List<Task> { _twitchBadgeDataProvider.TryRequestGlobalResources() };
				initTasks.AddRange(userIds.Select(userId => _twitchBadgeDataProvider.TryRequestChannelResources(userId)));

				if (twitchConfig.ParseCheermotes)
				{
					initTasks.Add(_twitchCheermoteDataProvider.TryRequestGlobalResources());
					initTasks.AddRange(userIds.Select(userId => _twitchCheermoteDataProvider.TryRequestChannelResources(userId)));
				}

				await Task.WhenAll(initTasks).ConfigureAwait(false);
			});
		}

		private void TwitchChannelManagementServiceOnChannelsUpdated(object sender, TwitchChannelsUpdatedEventArgs e)
		{
			_ = Task.Run(() =>
			{
				foreach (var disabledChannel in e.DisabledChannels)
				{
					ReleaseChannelResources(disabledChannel.Key);
				}

				Task.WhenAll(e.EnabledChannels.Select(enabledChannel => TryRequestChannelResources(enabledChannel.Key)));
			});
		}

		internal async Task TryRequestGlobalResources()
		{
			var twitchConfig = _kittenSettingsService.Config.TwitchConfig;
			var initTasks = new List<Task> { _twitchBadgeDataProvider.TryRequestGlobalResources() };
			if (twitchConfig.ParseCheermotes)
			{
				initTasks.Add(_twitchCheermoteDataProvider.TryRequestGlobalResources());
			}

			if (twitchConfig.ParseBttvEmotes)
			{
				initTasks.Add(_bttvDataProvider.TryRequestGlobalBttvResources());
			}

			if (twitchConfig.ParseFfzEmotes)
			{
				initTasks.Add(_bttvDataProvider.TryRequestGlobalFfzResources());
			}

			await Task.WhenAll(initTasks);
		}

		internal async Task TryRequestChannelResources(string userId)
		{
			var twitchConfig = _kittenSettingsService.Config.TwitchConfig;
			var initTasks = new List<Task> { _twitchBadgeDataProvider.TryRequestChannelResources(userId) };
			if (twitchConfig.ParseCheermotes)
			{
				initTasks.Add(_twitchCheermoteDataProvider.TryRequestChannelResources(userId));
			}

			if (twitchConfig.ParseBttvEmotes)
			{
				initTasks.Add(_bttvDataProvider.TryRequestBttvChannelResources(userId));
			}

			if (twitchConfig.ParseFfzEmotes)
			{
				initTasks.Add(_bttvDataProvider.TryRequestFfzChannelResources(userId));
			}

			await Task.WhenAll(initTasks);
		}

		internal void ReleaseChannelResources(string userId)
		{
			_twitchBadgeDataProvider.ReleaseChannelResources(userId);
			_twitchCheermoteDataProvider.ReleaseChannelResources(userId);
			_bttvDataProvider.ReleaseChannelResources(userId);
		}
	}
}