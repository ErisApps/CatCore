using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CatCore.Models.Credentials;
using CatCore.Models.EventArgs;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;

namespace CatCore.Services.Twitch
{
	public sealed class TwitchChannelManagementService : ITwitchChannelManagementService
	{
		private readonly IKittenSettingsService _kittenSettingsService;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchHelixApiService _twitchHelixApiService;
		private readonly Lazy<ITwitchIrcService> _twitchIrcService;

		public event EventHandler<TwitchChannelsUpdatedEventArgs>? ChannelsUpdated;

		internal TwitchChannelManagementService(IKittenSettingsService kittenSettingsService, ITwitchAuthService twitchAuthService, ITwitchHelixApiService twitchHelixApiService,
			Lazy<ITwitchIrcService> twitchIrcService)
		{
			_kittenSettingsService = kittenSettingsService;
			_twitchAuthService = twitchAuthService;
			_twitchHelixApiService = twitchHelixApiService;
			_twitchIrcService = twitchIrcService;
		}

		public TwitchChannel? GetOwnChannel()
		{
			if (_twitchAuthService.Status != AuthenticationStatus.Authenticated)
			{
				return null;
			}

			var self = _twitchAuthService.FetchLoggedInUserInfo();
			return self != null && _kittenSettingsService.Config.TwitchConfig.OwnChannelEnabled ? CreateChannel(self.Value.UserId, self.Value.LoginName) : null;
		}

		public List<string> GetAllActiveChannelIds(bool includeSelfRegardlessOfState = false)
		{
			var allChannels = new List<string>();
			var self = _twitchAuthService.FetchLoggedInUserInfo();
			var twitchConfig = _kittenSettingsService.Config.TwitchConfig;

			if (self != null && (twitchConfig.OwnChannelEnabled || includeSelfRegardlessOfState))
			{
				allChannels.Add(self.Value.UserId);
			}

			allChannels.AddRange(twitchConfig.AdditionalChannelsData.Keys);

			return allChannels;
		}

		public List<string> GetAllActiveLoginNames(bool includeSelfRegardlessOfState = false)
		{
			var allChannels = new List<string>();
			var self = _twitchAuthService.FetchLoggedInUserInfo();
			var twitchConfig = _kittenSettingsService.Config.TwitchConfig;

			if (self != null && (twitchConfig.OwnChannelEnabled || includeSelfRegardlessOfState))
			{
				allChannels.Add(self.Value.LoginName);
			}

			allChannels.AddRange(twitchConfig.AdditionalChannelsData.Values);

			return allChannels;
		}

		public ReadOnlyDictionary<string, string> GetAllActiveChannelsAsDictionary(bool includeSelfRegardlessOfState = false)
		{
			var allChannels = new Dictionary<string, string>();
			var self = _twitchAuthService.FetchLoggedInUserInfo();
			var twitchConfig = _kittenSettingsService.Config.TwitchConfig;

			if (self != null && (twitchConfig.OwnChannelEnabled || includeSelfRegardlessOfState))
			{
				allChannels.Add(self.Value.UserId, self.Value.LoginName);
			}

			foreach (var kvp in twitchConfig.AdditionalChannelsData)
			{
				allChannels.Add(kvp.Key, kvp.Value);
			}

			return new ReadOnlyDictionary<string, string>(allChannels);
		}

		public List<TwitchChannel> GetAllActiveChannels(bool includeSelfRegardlessOfState = false)
		{
			var allChannels = new List<TwitchChannel>();
			var self = _twitchAuthService.FetchLoggedInUserInfo();
			var twitchConfig = _kittenSettingsService.Config.TwitchConfig;

			if (self != null && (twitchConfig.OwnChannelEnabled || includeSelfRegardlessOfState))
			{
				allChannels.Add(CreateChannel(self.Value.UserId, self.Value.LoginName));
			}

			allChannels.AddRange(twitchConfig.AdditionalChannelsData.Select(kvp => CreateChannel(kvp.Key, kvp.Value)));

			return allChannels;
		}

		public async Task<List<UserData>> GetAllChannelsEnriched()
		{
			var allActiveChannelIds = GetAllActiveChannelIds(true);
			var userInfos = await _twitchHelixApiService.FetchUserInfo(allActiveChannelIds.ToArray()).ConfigureAwait(false);
			return userInfos?.Data ?? new List<UserData>();
		}

		public TwitchChannel CreateChannel(string channelId, string channelName)
		{
			return new TwitchChannel(_twitchIrcService.Value, channelId, channelName);
		}

		void ITwitchChannelManagementService.UpdateChannels(bool ownChannelActive, Dictionary<string, string> additionalChannelsData)
		{
			var enabledChannels = new Dictionary<string, string>();
			var disabledChannels = new Dictionary<string, string>();

			var twitchConfig = _kittenSettingsService.Config.TwitchConfig;

			var loggedInUserInfo = _twitchAuthService.FetchLoggedInUserInfo();
			if (loggedInUserInfo != null && twitchConfig.OwnChannelEnabled != ownChannelActive)
			{
				twitchConfig.OwnChannelEnabled = ownChannelActive;
				(ownChannelActive ? enabledChannels : disabledChannels).Add(loggedInUserInfo.Value.UserId, loggedInUserInfo.Value.LoginName);
			}

			var twitchChannelData = twitchConfig.AdditionalChannelsData;

			foreach (var keyValuePair in twitchChannelData.Except(additionalChannelsData))
			{
				disabledChannels.Add(keyValuePair.Key, keyValuePair.Value);
			}

			foreach (var keyValuePair in additionalChannelsData.Except(twitchChannelData))
			{
				enabledChannels.Add(keyValuePair.Key, keyValuePair.Value);
			}

			twitchConfig.AdditionalChannelsData = additionalChannelsData;

			ChannelsUpdated?.Invoke(this, new TwitchChannelsUpdatedEventArgs(enabledChannels, disabledChannels));
		}
	}
}