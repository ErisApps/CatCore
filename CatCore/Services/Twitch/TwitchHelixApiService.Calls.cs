using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Models.Twitch.Helix.Requests;
using CatCore.Models.Twitch.Helix.Responses;

namespace CatCore.Services.Twitch
{
	public partial class TwitchHelixApiService
	{
		public Task<ResponseBase<UserData>?> FetchUserInfo(CancellationToken? cancellationToken = null, params string[] loginNames)
		{
			var uriBuilder = new StringBuilder($"{TWITCH_HELIX_BASEURL}users");
			if (loginNames.Any())
			{
				uriBuilder.Append($"?login={loginNames.First()}");
				for (var i = 1; i < loginNames.Length; i++)
				{
					var loginName = loginNames[i];
					uriBuilder.Append($"&login={loginName}");
				}
			}

			return GetAsyncS<ResponseBase<UserData>>(uriBuilder.ToString(), cancellationToken);
		}

		public Task<ResponseBase<CreateStreamMarkerData>?> CreateStreamMarker(string userId, string? description = null, CancellationToken? cancellationToken = null)
		{
			// add description validation, max 140 chars
			if (!string.IsNullOrWhiteSpace(description) && description!.Length > 140)
			{
				throw new ArgumentException("The description argument is enforced to be 140 characters tops by Helix. Please use a shorter one.", nameof(description));
			}

			var body = new CreateStreamMarkerRequestDto(userId, description);
			return PostAsyncS<ResponseBase<CreateStreamMarkerData>, CreateStreamMarkerRequestDto>($"{TWITCH_HELIX_BASEURL}streams/markers", body, cancellationToken);
		}

		public Task<ResponseBaseWithPagination<ChannelData>?> SearchChannels(string query, uint? limit = null, bool? liveOnly = null, string? continuationCursor = null,
			CancellationToken? cancellationToken = null)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				throw new ArgumentException("The query parameter should not be null, empty or whitespace.", nameof(query));
			}

			var urlBuilder = new StringBuilder($"{TWITCH_HELIX_BASEURL}search/channels?query={query}");
			if (limit != null)
			{
				if (limit.Value > 100)
				{
					throw new ArgumentException("The limit parameter has an upper-limit of 100.", nameof(limit));
				}

				urlBuilder.Append($"&first={limit}");
			}

			if (liveOnly != null)
			{
				urlBuilder.Append($"live_only={liveOnly}");
			}

			if (continuationCursor != null)
			{
				if (string.IsNullOrWhiteSpace(query))
				{
					throw new ArgumentException("The continuationCursor parameter should not be null, empty or whitespace.", nameof(continuationCursor));
				}

				urlBuilder.Append($"after={continuationCursor}");
			}

			return GetAsyncS<ResponseBaseWithPagination<ChannelData>>(urlBuilder.ToString(), cancellationToken);
		}
	}
}