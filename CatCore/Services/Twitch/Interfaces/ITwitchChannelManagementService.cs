using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CatCore.Models.EventArgs;
using CatCore.Models.Twitch.Helix.Responses;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchChannelManagementService
	{
		event EventHandler<TwitchChannelsUpdatedEventArgs>? ChannelsUpdated;

		List<string> GetAllActiveLoginNames(bool includeSelfRegardlessOfState = false);
		List<string> GetAllActiveChannelIds(bool includeSelfRegardlessOfState = false);
		Task<List<UserData>> GetAllChannelsEnriched();

		internal void UpdateChannels(bool ownChannelActive, Dictionary<string, string> additionalChannelsData);
	}
}