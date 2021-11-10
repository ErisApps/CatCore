using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes
{
	public readonly struct CheermoteImageSizeGroup
	{
		[JsonPropertyName("1")]
		public string Size1 { get; }

		[JsonPropertyName("1.5")]
		public string Size15 { get; }

		[JsonPropertyName("2")]
		public string Size2 { get; }

		[JsonPropertyName("3")]
		public string Size3 { get; }

		[JsonPropertyName("4")]
		public string Size4 { get; }

		[JsonConstructor]
		public CheermoteImageSizeGroup(string size1, string size15, string size2, string size3, string size4)
		{
			Size1 = size1;
			Size15 = size15;
			Size2 = size2;
			Size3 = size3;
			Size4 = size4;
		}
	}
}