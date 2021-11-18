using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv
{
	public readonly struct BttvEmoteUser
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("name")]
		public string Name { get; }

		[JsonPropertyName("displayName")]
		public string DisplayName { get; }

		[JsonPropertyName("providerId")]
		public string ProviderId { get; }

		[JsonConstructor]
		public BttvEmoteUser(string id, string name, string displayName, string providerId)
		{
			Id = id;
			Name = name;
			DisplayName = displayName;
			ProviderId = providerId;
		}
	}
}