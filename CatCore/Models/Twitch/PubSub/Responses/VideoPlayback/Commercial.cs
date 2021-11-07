namespace CatCore.Models.Twitch.PubSub.Responses.VideoPlayback
{
	public sealed class Commercial : VideoPlaybackBase
	{
		public uint Length { get; }

		public Commercial(string serverTimeRaw, uint length) : base(serverTimeRaw)
		{
			Length = length;
		}
	}
}