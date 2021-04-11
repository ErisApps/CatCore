using System.Collections.Generic;
using System.Threading.Tasks;
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

		internal TwitchChannelManagementService(IKittenSettingsService kittenSettingsService, ITwitchAuthService twitchAuthService, ITwitchHelixApiService twitchHelixApiService)
		{
			_kittenSettingsService = kittenSettingsService;
			_twitchAuthService = twitchAuthService;
			_twitchHelixApiService = twitchHelixApiService;
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
			var allLoginNames = GetAllActiveLoginNames(true);
			var userInfos = await _twitchHelixApiService.FetchUserInfo(loginNames: allLoginNames.ToArray()).ConfigureAwait(false);
			return userInfos?.Data ?? new List<UserData>();
		}
	}
}