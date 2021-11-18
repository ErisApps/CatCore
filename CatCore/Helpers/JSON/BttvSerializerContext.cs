using System.Text.Json.Serialization;
using CatCore.Models.ThirdParty.Bttv;

namespace CatCore.Helpers.JSON
{
	[JsonSerializable(typeof(BttvEmote))]
	[JsonSerializable(typeof(BttvChannelData))]
	internal partial class BttvSerializerContext : JsonSerializerContext
	{
	}
}