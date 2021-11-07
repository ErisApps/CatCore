using CatCore.Models.Shared;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.IRC
{
	public sealed class TwitchEmote : IChatEmote
	{
		[PublicAPI]
		public string Id { get; }
		public string Name { get; }
		public int StartIndex { get; }
		public int EndIndex { get; }
		public string Url { get; }
		public bool Animated { get; }

		public TwitchEmote(string id, string name, int startIndex, int endIndex, string url)
		{
			Id = "TwitchEmote_" + id;
			Name = name;
			StartIndex = startIndex;
			EndIndex = endIndex;
			Url = url;
			Animated = false;
		}

		// TODO: figure out a way to pass on animated urls when available?
		public TwitchEmote(string id, string name, int startIndex, int endIndex, string url, bool animated)
		{
			Id = "TwitchEmote_" + id;
			Name = name;
			StartIndex = startIndex;
			EndIndex = endIndex;
			Url = url;
			Animated = animated;
		}
	}
}