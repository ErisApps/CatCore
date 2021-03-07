using System.Threading;
using System.Threading.Tasks;
using CatCore.Models.Twitch.Helix.Responses;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchHelixApiService
	{
		Task<ResponseBase<UserData>?> FetchUserInfo(CancellationToken? cancellationToken = null, params string[] loginNames);
		Task<ResponseBase<CreateStreamMarkerData>?> CreateStreamMarker(string userId, string? description = null, CancellationToken? cancellationToken = null);
	}
}