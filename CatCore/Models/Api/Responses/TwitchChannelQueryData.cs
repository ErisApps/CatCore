using CatCore.Models.Twitch.Helix.Responses;

namespace CatCore.Models.Api.Responses
{
	internal readonly struct TwitchChannelQueryData
	{
		public string ThumbnailUrl { get; }
		public string DisplayName { get; }
		public string LoginName { get; }
		public string ChannelId { get; }

		public TwitchChannelQueryData(ChannelData channelData)
		{
			ThumbnailUrl = channelData.ThumbnailUrl;
			DisplayName = channelData.DisplayName;
			LoginName = channelData.BroadcasterLogin;
			ChannelId = channelData.ChannelId;
		}

		public TwitchChannelQueryData(UserData userData)
		{
			ThumbnailUrl = userData.ProfileImageUrl;
			DisplayName = userData.DisplayName;
			LoginName = userData.LoginName;
			ChannelId = userData.UserId;
		}
	}
}