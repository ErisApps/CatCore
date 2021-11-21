using CatCore.Models.Shared;

namespace CatCore.Models.Twitch.Media
{
	public sealed class TwitchCheermoteData : IChatResourceData
	{
		private const string TYPE = "TwitchCheermote";

		public string Id { get; }
		public string Name { get; }
		public string Url { get; }
		public bool IsAnimated { get; }
		public uint MinBits { get; }
		public string Color { get; }
		public bool CanCheer { get; }
		public string Type => TYPE;

		public TwitchCheermoteData(string name, string url, bool isAnimated, uint minBits, string color, bool canCheer)
		{
			Id = Type + "_" + name;
			Name = name;
			Url = url;
			IsAnimated = isAnimated;
			MinBits = minBits;
			Color = color;
			CanCheer = canCheer;
		}
	}
}