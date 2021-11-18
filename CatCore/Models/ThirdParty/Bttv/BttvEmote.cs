using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv
{
	public sealed class BttvEmote : BttvEmoteBase
	{
		[JsonPropertyName("userId")]
		public string UserId { get; }

		[JsonConstructor]
		public BttvEmote(string id, string code, string imageType, string userId) : base(id, code, imageType)
		{
			UserId = userId;
		}
	}
}