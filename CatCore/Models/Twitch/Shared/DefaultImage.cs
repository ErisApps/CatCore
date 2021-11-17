using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Shared
{
	public readonly struct DefaultImage
	{
		[JsonPropertyName("url_1x")]
		public string Url1X { get; }

		[JsonPropertyName("url_2x")]
		public string Url2X { get; }

		[JsonPropertyName("url_4x")]
		public string Url4X { get; }

		[JsonConstructor]
		public DefaultImage(string url1X, string url2X, string url4X)
		{
			Url1X = url1X;
			Url2X = url2X;
			Url4X = url4X;
		}
	}
}