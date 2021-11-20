using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv
{
	public sealed class BttvSharedEmote : BttvEmoteBase
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