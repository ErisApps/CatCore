using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv.Base;

namespace CatCore.Models.ThirdParty.Bttv.Ffz
{
	public sealed class FfzEmoteUser : EmoteUserBase
	{
		[JsonPropertyName("id")]
		public uint Id { get; }

		[JsonConstructor]
		public FfzEmoteUser(uint id, string name, string displayName) : base(name, displayName)
		{
			Id = id;
		}
	}
}