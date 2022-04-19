using System.Collections.Generic;
using System.Linq;
using CatCore.Models.Config;
using CatCore.Models.Twitch.Helix.Responses;
using CatCore.Models.Twitch.OAuth;

namespace CatCore.Models.Api.Responses
{
	internal readonly struct TwitchStateResponseDto
	{
		public bool LoggedIn { get; }
		public bool OwnChannelEnabled { get; }

		public List<TwitchChannelData> ChannelData { get; }

		public bool ParseBttvEmotes { get; }
		public bool ParseFfzEmotes { get; }
		public bool ParseTwitchEmotes { get; }
		public bool ParseCheermotes { get; }

		public TwitchStateResponseDto(bool isValid, ValidationResponse? loggedInUser, IEnumerable<UserData>? channelData, TwitchConfig twitchConfig)
		{
			LoggedIn = isValid;

			OwnChannelEnabled = twitchConfig.OwnChannelEnabled;
			ChannelData = channelData?
				.Select(x => new TwitchChannelData(x.ProfileImageUrl, x.DisplayName, x.LoginName, x.UserId, x.LoginName == loggedInUser?.LoginName))
				.OrderByDescending(channel => channel.IsSelf)
				.ThenBy(channel => channel.DisplayName)
				.ToList() ?? new List<TwitchChannelData>();

			ParseBttvEmotes = twitchConfig.ParseBttvEmotes;
			ParseFfzEmotes = twitchConfig.ParseFfzEmotes;
			ParseTwitchEmotes = twitchConfig.ParseTwitchEmotes;
			ParseCheermotes = twitchConfig.ParseCheermotes;
		}
	}
}