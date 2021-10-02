using System.Runtime.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes
{
	public enum CheermoteType
	{
		[EnumMember(Value = "global_first_party")]
		GlobalFirstParty,

		[EnumMember(Value = "global_third_party")]
		GlobalThirdParty,

		[EnumMember(Value = "channel_custom")]
		ChannelCustom,

		[EnumMember(Value = "display_only")]
		DisplayOnly,

		[EnumMember(Value = "sponsored")]
		Sponsored
	}
}