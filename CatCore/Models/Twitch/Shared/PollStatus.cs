using System.Runtime.Serialization;

namespace CatCore.Models.Twitch.Shared
{
	public enum PollStatus
	{
		[EnumMember(Value = "ACTIVE")]
		Active,

		[EnumMember(Value = "COMPLETED")]
		Completed,

		[EnumMember(Value = "TERMINATED")]
		Terminated,

		[EnumMember(Value = "ARCHIVED")]
		Archived,

		[EnumMember(Value = "MODERATED")]
		Moderated,

		[EnumMember(Value = "INVALID")]
		Invalid
	}
}