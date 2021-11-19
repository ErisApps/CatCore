using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv.Base;

namespace CatCore.Models.ThirdParty.Bttv.Ffz
{
	public sealed class FfzEmoteUser : EmoteUserBase
	{
		[JsonConstructor]
		public FfzEmoteUser(string id, string name, string displayName) : base(id, name, displayName)
		{
		}
	}
}