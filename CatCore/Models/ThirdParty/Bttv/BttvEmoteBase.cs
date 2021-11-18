using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv
{
	public abstract class BttvEmoteBase
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("code")]
		public string Code { get; }

		[JsonPropertyName("imageType")]
		public string ImageType { get; }

		public BttvEmoteBase(string id, string code, string imageType)
		{
			Id = id;
			Code = code;
			ImageType = imageType;
		}
	}
}