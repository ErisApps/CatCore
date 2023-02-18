using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatCore.Models.Config;
using CatCore.Models.Credentials;
using CatCore.Models.EventArgs;
using CatCore.Models.Shared;
using CatCore.Models.Twitch.Media;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;

namespace CatCore.Services.Twitch.Media
{
	internal class TwitchMediaDataProvider
	{
		private readonly IKittenSettingsService _kittenSettingsService;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchChannelManagementService _twitchChannelManagementService;
		private readonly TwitchBadgeDataProvider _twitchBadgeDataProvider;
		private readonly TwitchCheermoteDataProvider _twitchCheermoteDataProvider;
		private readonly BttvDataProvider _bttvDataProvider;

		private bool _cheermotesEnabled;
		private bool _bttvEnabled;
		private bool _ffzEnabled;

		public TwitchMediaDataProvider(IKittenSettingsService kittenSettingsService, ITwitchAuthService twitchAuthService,
			ITwitchChannelManagementService twitchChannelManagementService, TwitchBadgeDataProvider twitchBadgeDataProvider, TwitchCheermoteDataProvider twitchCheermoteDataProvider,
			BttvDataProvider bttvDataProvider)
		{
			_kittenSettingsService = kittenSettingsService;
			_twitchAuthService = twitchAuthService;
			_twitchChannelManagementService = twitchChannelManagementService;

			_twitchBadgeDataProvider = twitchBadgeDataProvider;
			_twitchCheermoteDataProvider = twitchCheermoteDataProvider;
			_bttvDataProvider = bttvDataProvider;

			_kittenSettingsService.OnConfigChanged += KittenSettingsServiceOnConfigChanged;
			_twitchAuthService.OnAuthenticationStatusChanged += TwitchAuthServiceOnAuthenticationStatusChanged;
			_twitchChannelManagementService.ChannelsUpdated += TwitchChannelManagementServiceOnChannelsUpdated;

			var twitchConfig = kittenSettingsService.Config.TwitchConfig;
			_cheermotesEnabled = twitchConfig.ParseCheermotes;
			_bttvEnabled = twitchConfig.ParseBttvEmotes;
			_ffzEnabled = twitchConfig.ParseFfzEmotes;
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

		private void TwitchAuthServiceOnAuthenticationStatusChanged(AuthenticationStatus status)
		{
			if (status != AuthenticationStatus.Authenticated)
			{
				return;
			}

			_ = Task.Run(async () =>
			{
				var initTasks = new List<Task> { TryRequestGlobalResources() };
				var allActiveChannelIds = _twitchChannelManagementService.GetAllActiveChannelIds();
				initTasks.AddRange(allActiveChannelIds.Select(userId => TryRequestChannelResources(userId)));
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

		internal bool TryGetBadge(string identifier, string userId, out TwitchBadge? badge)
		{
			return _twitchBadgeDataProvider.TryGetBadge(identifier, userId, out badge);
		}

		// TODO: Verify this implementation actually works...
		internal bool TryGetCheermote(string identifier, string userId, out uint emoteBits, out TwitchCheermoteData? cheermoteData)
		{
			emoteBits = 0;
			cheermoteData = null;

			if (!char.IsLetter(identifier[0]) || !char.IsDigit(identifier[identifier.Length - 1]))
			{
				return false;
			}

			var prefixLength = identifier.Length - 1;
			// Starting at length - 2 because length - 1 is already known to be a digit
			for (var i = identifier.Length - 2; i >= 0; i--)
			{
				if (char.IsDigit(identifier[i]))
				{
					continue;
				}

				prefixLength = i + 1;
				break;
			}

			return uint.TryParse(identifier.Substring(prefixLength), out emoteBits) && _twitchCheermoteDataProvider.TryGetCheermote(identifier.Substring(0, prefixLength), userId, emoteBits, out cheermoteData);
		}

		internal bool TryGetThirdPartyEmote(string identifier, string userId, out ChatResourceData? customEmote)
		{
			if (_bttvEnabled && _bttvDataProvider.TryGetBttvEmote(identifier, userId, out customEmote) ||
			    _ffzEnabled && _bttvDataProvider.TryGetFfzEmote(identifier, userId, out customEmote))
			{
				return true;
			}

			customEmote = null;
			return false;
		}
	}
}