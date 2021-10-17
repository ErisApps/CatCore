using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Badges
{
	public readonly struct BadgeData
	{
		[JsonPropertyName("set_id")]
		public string SetId { get; }

		[JsonPropertyName("versions")]
		public IReadOnlyList<BadgeVersion> Versions { get; }

		[JsonConstructor]
		public BadgeData(string setId, IReadOnlyList<BadgeVersion> versions)
		{
			SetId = setId;
			Versions = versions;
		}
	}
}