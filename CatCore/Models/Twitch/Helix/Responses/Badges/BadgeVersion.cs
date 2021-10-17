using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Badges
{
	public readonly struct BadgeVersion
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("image_url_1x")]
		public string ImageUrl1x { get; }

		[JsonPropertyName("image_url_2x")]
		public string ImageUrl2x { get; }

		[JsonPropertyName("image_url_4x")]
		public string ImageUrl4x { get; }

		[JsonConstructor]
		public BadgeVersion(string id, string imageUrl1X, string imageUrl2X, string imageUrl4X)
		{
			Id = id;
			ImageUrl1x = imageUrl1X;
			ImageUrl2x = imageUrl2X;
			ImageUrl4x = imageUrl4X;
		}
	}
}