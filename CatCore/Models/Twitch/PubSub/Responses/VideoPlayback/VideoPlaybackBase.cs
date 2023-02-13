using System;
using System.Threading;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.PubSub.Responses.VideoPlayback
{
	[PublicAPI]
	public abstract class VideoPlaybackBase
	{
		private const long TICKS_PER_MICROSECOND = TimeSpan.TicksPerMillisecond / 1000L;

		public string ServerTimeRaw { get; }
		public Lazy<DateTimeOffset> ServerTime { get; }

		protected VideoPlaybackBase(string serverTimeRaw)
		{
			ServerTimeRaw = serverTimeRaw;
			ServerTime = new(() => InitializeServerTime(), LazyThreadSafetyMode.PublicationOnly);
		}

		private DateTimeOffset InitializeServerTime()
		{
			// TODO: Look into fixed index (10)
			// Will only break starting Saturday, November 20, 2286 05:46:40 PM UTC
			// Actually might break already on Tuesday, January 19, 2038 03:14:08 AM UTC :|
			var dotIndex = ServerTimeRaw.IndexOf('.');
			var serverTimeMicrosRaw = ServerTimeRaw.Remove(dotIndex, 1);
			var serverTimeMicros = long.Parse(serverTimeMicrosRaw);
			return DateTimeOffset.MinValue.AddTicks(serverTimeMicros * TICKS_PER_MICROSECOND);
		}
	}
}