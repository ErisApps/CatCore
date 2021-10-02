using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes
{
	public class CheermoteTier
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("min_bits")]
		public int MinBits { get; }

		[JsonPropertyName("color")]
		public string Color { get; }

		[JsonPropertyName("images")]
		public CheermoteImageColorGroup Images { get; }

		[JsonPropertyName("can_cheer")]
		public bool CanCheer { get; }

		[JsonPropertyName("show_in_bits_card")]
		public bool ShowInBitsCard { get; }

		[JsonConstructor]
		public CheermoteTier(string id, int minBits, string color, CheermoteImageColorGroup images, bool canCheer, bool showInBitsCard)
		{
			Id = id;
			MinBits = minBits;
			Color = color;
			Images = images;
			CanCheer = canCheer;
			ShowInBitsCard = showInBitsCard;
		}
	}
}