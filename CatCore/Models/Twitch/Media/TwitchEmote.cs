using CatCore.Models.Shared;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.Media
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
		public uint Bits { get; }
		public string? Color { get; }

		public TwitchEmote(string id, string name, int startIndex, int endIndex, string url)
		{
			Id = id;
			Name = name;
			StartIndex = startIndex;
			EndIndex = endIndex;
			Url = url;
			Animated = false;
			Bits = 0;
		}

		public TwitchEmote(string id, string name, int startIndex, int endIndex, string url, bool animated)
		{
			Id = id;
			Name = name;
			StartIndex = startIndex;
			EndIndex = endIndex;
			Url = url;
			Animated = animated;
			Bits = 0;
		}

		public TwitchEmote(string id, string name, int startIndex, int endIndex, string url, bool animated, uint bits, string color)
		{
			Id = id;
			Name = name;
			StartIndex = startIndex;
			EndIndex = endIndex;
			Url = url;
			Animated = animated;
			Bits = bits;
			Color = color;
		}
	}
}