using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CatCore.Models.EventArgs;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Services.Interfaces;
using CatCore.Services.Twitch.Interfaces;

namespace CatCore.Services.Twitch
{
	public class TwitchChannelManagementService : ITwitchChannelManagementService
	{
		private readonly IKittenSettingsService _kittenSettingsService;
		private readonly ITwitchAuthService _twitchAuthService;
		private readonly ITwitchHelixApiService _twitchHelixApiService;

		public event EventHandler<TwitchChannelsUpdatedEventArgs>? ChannelsUpdated;

		internal TwitchChannelManagementService(IKittenSettingsService kittenSettingsService, ITwitchAuthService twitchAuthService, ITwitchHelixApiService twitchHelixApiService)
		{
			_kittenSettingsService = kittenSettingsService;
			_twitchAuthService = twitchAuthService;
			_twitchHelixApiService = twitchHelixApiService;
		}

		public TwitchChannel? GetOwnChannel()
		{
			var self = _twitchAuthService.LoggedInUser;
			return self != null && _kittenSettingsService.Config.TwitchConfig.OwnChannelEnabled ? new TwitchChannel(self.Value.UserId, self.Value.LoginName) : null;
		}

		public List<string> GetAllActiveLoginNames(bool includeSelfRegardlessOfState = false)
		{
			var allChannels = new List<string>();
			var self = _twitchAuthService.LoggedInUser;
			if (self != null && (_kittenSettingsService.Config.TwitchConfig.OwnChannelEnabled || includeSelfRegardlessOfState))
			{
				allChannels.Add(self.Value.LoginName);
			}

			allChannels.AddRange(_kittenSettingsService.Config.TwitchConfig.AdditionalChannelsData.Values);

			return allChannels;
		}

		public List<string> GetAllActiveChannelIds(bool includeSelfRegardlessOfState = false)
		{
			var allChannels = new List<string>();
			var self = _twitchAuthService.LoggedInUser;
			if (self != null && (_kittenSettingsService.Config.TwitchConfig.OwnChannelEnabled || includeSelfRegardlessOfState))
			{
				allChannels.Add(self.Value.UserId);
			}

			allChannels.AddRange(_kittenSettingsService.Config.TwitchConfig.AdditionalChannelsData.Keys);

			return allChannels;
		}

		public async Task<List<UserData>> GetAllChannelsEnriched()
		{
			var allActiveChannelIds = GetAllActiveChannelIds(true);
			var userInfos = await _twitchHelixApiService.FetchUserInfo(allActiveChannelIds.ToArray()).ConfigureAwait(false);
			return userInfos?.Data ?? new List<UserData>();
		}

		void ITwitchChannelManagementService.UpdateChannels(bool ownChannelActive, Dictionary<string, string> additionalChannelsData)
		{
			Dictionary<string, string> enabledChannels = new Dictionary<string, string>();
			Dictionary<string, string> disabledChannels = new Dictionary<string, string>();

			if (_twitchAuthService.LoggedInUser != null && _kittenSettingsService.Config.TwitchConfig.OwnChannelEnabled != ownChannelActive)
			{
				_kittenSettingsService.Config.TwitchConfig.OwnChannelEnabled = ownChannelActive;
				(ownChannelActive ? enabledChannels : disabledChannels).Add(_twitchAuthService.LoggedInUser.Value.UserId, _twitchAuthService.LoggedInUser.Value.LoginName);
			}

			var twitchChannelData = _kittenSettingsService.Config.TwitchConfig.AdditionalChannelsData;

			foreach (var keyValuePair in twitchChannelData.Except(additionalChannelsData))
			{
				disabledChannels.Add(keyValuePair.Key, keyValuePair.Value);
			}

			foreach (var keyValuePair in additionalChannelsData.Except(twitchChannelData))
			{
				enabledChannels.Add(keyValuePair.Key, keyValuePair.Value);
			}

			_kittenSettingsService.Config.TwitchConfig.AdditionalChannelsData = additionalChannelsData;

			ChannelsUpdated?.Invoke(this, new TwitchChannelsUpdatedEventArgs(enabledChannels, disabledChannels));
		}
	}
}