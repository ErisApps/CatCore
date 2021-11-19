using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv;
using CatCore.Models.ThirdParty.Bttv.Ffz;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(IReadOnlyList<BttvEmote>))]
	[JsonSerializable(typeof(BttvChannelData))]
	[JsonSerializable(typeof(IReadOnlyList<FfzEmote>))]
	internal partial class BttvSerializerContext : JsonSerializerContext
	{
	}
}