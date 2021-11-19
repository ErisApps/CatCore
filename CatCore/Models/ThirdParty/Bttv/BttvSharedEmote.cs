using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv.Base;

namespace CatCore.Models.ThirdParty.Bttv
{
	public sealed class BttvSharedEmote : EmoteBase
	{
		[JsonPropertyName("user")]
		public BttvEmoteUser User { get; }

		[JsonConstructor]
		public BttvSharedEmote(string id, string code, string imageType, BttvEmoteUser user) : base(id, code, imageType)
		{
			User = user;
		}
	}
}