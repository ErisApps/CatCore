namespace CatCore.Models.Twitch.PubSub.Responses.VideoPlayback
{
	public sealed class StreamDown : VideoPlaybackBase
	{
		public StreamDown(string serverTimeRaw) : base(serverTimeRaw)
		{
		}
	}
}