namespace CatCore
{
	internal partial class Constants : ConstantsBase
	{
	}

	internal abstract class ConstantsBase
	{
		internal static string InternalApiServerUri => "http://localhost:8338/";

		internal abstract string TwitchClientId { get; }
		internal abstract string TwitchClientSecret { get; }
	}
}