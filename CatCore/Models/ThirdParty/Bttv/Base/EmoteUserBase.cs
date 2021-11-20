using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv.Base
{
	public abstract class EmoteUserBase
	{
		[JsonPropertyName("name")]
		public string Name { get; }

		[JsonPropertyName("displayName")]
		public string DisplayName { get; }

		[JsonConstructor]
		public EmoteUserBase(string name, string displayName)
		{
			Name = name;
			DisplayName = displayName;
		}
	}
}