using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes
{
	public readonly struct CheermoteImageColorGroup
	{
		[JsonPropertyName("light")]
		public CheermoteImageTypesGroup Light { get; }

		[JsonPropertyName("dark")]
		public CheermoteImageTypesGroup Dark { get; }

		[JsonConstructor]
		public CheermoteImageColorGroup(CheermoteImageTypesGroup light, CheermoteImageTypesGroup dark)
		{
			Light = light;
			Dark = dark;
		}
	}
}