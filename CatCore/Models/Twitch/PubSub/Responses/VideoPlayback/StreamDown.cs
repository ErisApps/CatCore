using JetBrains.Annotations;

namespace CatCore.Models.Twitch.PubSub.Responses.VideoPlayback
{
	[PublicAPI]
	public sealed class StreamDown : VideoPlaybackBase
	{
		public StreamDown(string serverTimeRaw) : base(serverTimeRaw)
		{
		}
	}
}