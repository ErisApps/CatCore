using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv.Base;

namespace CatCore.Models.ThirdParty.Bttv.Ffz
{
	public sealed class FfzEmote : EmoteBase
	{
		[JsonPropertyName("user")]
		public FfzEmoteUser User { get; }

		[JsonPropertyName("images")]
		public FfzImageSizes Images { get; }

		[JsonConstructor]
		public FfzEmote(string id, string code, string imageType, FfzEmoteUser user, FfzImageSizes images) : base(id, code, imageType)
		{
			User = user;
			Images = images;
		}
	}
}