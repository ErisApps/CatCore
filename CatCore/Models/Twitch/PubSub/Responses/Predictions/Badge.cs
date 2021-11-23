using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Predictions
{
	public readonly struct Badge
	{
		[JsonPropertyName("set_id")]
		public string SetId { get; }

		[JsonPropertyName("version")]
		public string Version { get; }

		[JsonConstructor]
		public Badge(string setId, string version)
		{
			SetId = setId;
			Version = version;
		}
	}
}