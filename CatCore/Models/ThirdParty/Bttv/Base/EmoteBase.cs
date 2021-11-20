using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv.Base
{
	public abstract class EmoteBase
	{
		[JsonPropertyName("code")]
		public string Code { get; }

		[JsonPropertyName("imageType")]
		public string ImageType { get; }

		[JsonConstructor]
		public EmoteBase(string code, string imageType)
		{
			Code = code;
			ImageType = imageType;
		}
	}
}