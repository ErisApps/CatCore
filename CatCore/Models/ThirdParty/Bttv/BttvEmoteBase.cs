using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv.Base;

namespace CatCore.Models.ThirdParty.Bttv
{
	public abstract class BttvEmoteBase : EmoteBase
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonConstructor]
		protected BttvEmoteBase(string id, string code, string imageType) : base(code, imageType)
		{
			Id = id;
		}
	}
}