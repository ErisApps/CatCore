namespace CatCore
{
	internal partial class Constants : ConstantsBase
	{
	}

	internal abstract class ConstantsBase
	{
		internal abstract string TwitchClientId { get; }
		internal abstract string TwitchClientSecret { get; }
	}
}