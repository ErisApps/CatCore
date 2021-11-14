using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CatCore.Models.EventArgs;
using CatCore.Models.Twitch;
using CatCore.Models.Twitch.Helix.Responses;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchChannelManagementService
	{
		event EventHandler<TwitchChannelsUpdatedEventArgs>? ChannelsUpdated;

		TwitchChannel? GetOwnChannel();

		List<string> GetAllActiveChannelIds(bool includeSelfRegardlessOfState = false);
		List<string> GetAllActiveLoginNames(bool includeSelfRegardlessOfState = false);
		List<TwitchChannel> GetAllActiveChannels(bool includeSelfRegardlessOfState = false);
		ReadOnlyDictionary<string, string> GetAllActiveChannelsAsDictionary(bool includeSelfRegardlessOfState = false);
		Task<List<UserData>> GetAllChannelsEnriched();

		TwitchChannel CreateChannel(string channelId, string channelName);

		internal void UpdateChannels(bool ownChannelActive, Dictionary<string, string> additionalChannelsData);
	}
}