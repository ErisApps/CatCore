namespace CatCore
{
	internal sealed partial class Constants : ConstantsBase
	{
	}

	internal abstract class ConstantsBase
	{
		internal static string InternalApiServerUri => "http://localhost:8338/";

		internal abstract string CatCoreAuthServerUri { get; }

		internal abstract string TwitchClientId { get; }
	}
}