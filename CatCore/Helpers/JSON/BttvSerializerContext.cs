using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv;
using CatCore.Models.ThirdParty.Bttv.Ffz;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(BttvEmote))]
	[JsonSerializable(typeof(BttvChannelData))]
	[JsonSerializable(typeof(FfzEmote))]
	internal partial class BttvSerializerContext : JsonSerializerContext
	{
	}
}