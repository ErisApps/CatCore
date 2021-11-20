using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv.Base;

namespace CatCore.Models.ThirdParty.Bttv
{
	public sealed class BttvEmoteUser : EmoteUserBase
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("providerId")]
		public string ProviderId { get; }

		[JsonConstructor]
		public BttvEmoteUser(string id, string name, string displayName, string providerId) : base(name, displayName)
		{
			Id = id;
			ProviderId = providerId;
		}
	}
}