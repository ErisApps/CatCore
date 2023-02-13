using JetBrains.Annotations;

namespace CatCore.Models.Twitch.PubSub.Responses.VideoPlayback
{
	[PublicAPI]
	public sealed class StreamUp : VideoPlaybackBase
	{
		public int PlayDelay { get; }

		public StreamUp(string serverTimeRaw, int playDelay) : base(serverTimeRaw)
		{
			PlayDelay = playDelay;
		}
	}
}