using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv.Ffz
{
	public readonly struct FfzImageSizes
	{
		[JsonPropertyName("1x")]
		public string? Url1X { get; }

		[JsonPropertyName("2x")]
		public string? Url2X { get; }

		[JsonPropertyName("4x")]
		public string? Url4X { get; }

		[JsonIgnore]
		public string? PreferredUrl => Url4X ?? Url2X ?? Url1X;

		[JsonConstructor]
		public FfzImageSizes(string url1X, string url2X, string url4X)
		{
			Url1X = url1X;
			Url2X = url2X;
			Url4X = url4X;
		}
	}
}