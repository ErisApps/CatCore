using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv.Base
{
	public abstract class EmoteBase
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("code")]
		public string Code { get; }

		[JsonPropertyName("imageType")]
		public string ImageType { get; }

		[JsonConstructor]
		public EmoteBase(string id, string code, string imageType)
		{
			Id = id;
			Code = code;
			ImageType = imageType;
		}
	}
}