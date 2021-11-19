using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv.Base
{
	public abstract class EmoteUserBase
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("name")]
		public string Name { get; }

		[JsonPropertyName("displayName")]
		public string DisplayName { get; }

		[JsonConstructor]
		public EmoteUserBase(string id, string name, string displayName)
		{
			Id = id;
			Name = name;
			DisplayName = displayName;
		}
	}
}