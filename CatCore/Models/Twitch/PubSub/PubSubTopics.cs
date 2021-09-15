namespace CatCore.Models.Twitch.PubSub
{
	internal static class PubSubTopics
	{
		public const string VIDEO_PLAYBACK = "video-playback-by-id";

		public static string FormatVideoPlaybackTopic(string channelId) => VIDEO_PLAYBACK + "." + channelId;
	}
}