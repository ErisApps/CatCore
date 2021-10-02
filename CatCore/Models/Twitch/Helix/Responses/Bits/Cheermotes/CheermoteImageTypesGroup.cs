using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes
{
	public class CheermoteImageTypesGroup
	{
		[JsonPropertyName("static")]
		public CheermoteImageSizeGroup Static { get; }

		[JsonPropertyName("animated")]
		public CheermoteImageSizeGroup Animated { get; }

		[JsonConstructor]
		public CheermoteImageTypesGroup(CheermoteImageSizeGroup @static, CheermoteImageSizeGroup animated)
		{
			Static = @static;
			Animated = animated;
		}
	}
}