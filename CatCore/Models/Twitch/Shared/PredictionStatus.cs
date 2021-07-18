using System.Runtime.Serialization;

namespace CatCore.Models.Twitch.Shared
{
	public enum PredictionStatus
	{
		[EnumMember(Value = "ACTIVE")]
		Active,

		[EnumMember(Value = "RESOLVED")]
		Resolved,

		[EnumMember(Value = "CANCELED")]
		Cancelled,

		[EnumMember(Value = "LOCKED")]
		Locked
	}
}