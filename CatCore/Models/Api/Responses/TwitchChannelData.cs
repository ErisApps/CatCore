namespace CatCore.Models.Api.Responses
{
	internal readonly struct TwitchChannelData
	{
		public string ThumbnailUrl { get; }
		public string DisplayName { get; }
		public string LoginName { get; }
		public string ChannelId { get; }
		public bool IsSelf { get; }

		public TwitchChannelData(string thumbnailUrl, string displayName, string loginName, string channelId, bool isSelf)
		{
			ThumbnailUrl = thumbnailUrl;
			DisplayName = displayName;
			LoginName = loginName;
			ChannelId = channelId;
			IsSelf = isSelf;
		}
	}
}