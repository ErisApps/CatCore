using System.Collections.Generic;
using System.Threading.Tasks;
using CatCore.Models.Twitch.Helix.Responses;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchChannelManagementService
	{
		List<string> GetAllActiveLoginNames(bool includeSelfRegardlessOfState);
		Task<List<UserData>> GetAllChannelsEnriched();
	}
}