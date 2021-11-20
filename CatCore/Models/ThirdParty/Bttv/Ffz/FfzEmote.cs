using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv.Base;

namespace CatCore.Models.ThirdParty.Bttv.Ffz
{
	public sealed class FfzEmote : EmoteBase
	{
		[JsonPropertyName("id")]
		public uint Id { get; }

		[JsonPropertyName("user")]
		public FfzEmoteUser User { get; }

		[JsonPropertyName("images")]
		public FfzImageSizes Images { get; }

		[JsonConstructor]
		public FfzEmote(uint id, string code, string imageType, FfzEmoteUser user, FfzImageSizes images) : base(code, imageType)
		{
			Id = id;
			User = user;
			Images = images;
		}
	}
}