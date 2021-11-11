namespace CatCore
{
	internal sealed partial class Constants : ConstantsBase
	{
		internal override string CatCoreAuthServerUri => throw new System.NotImplementedException();

		internal override string TwitchClientId => throw new System.NotImplementedException();
	}

	internal abstract class ConstantsBase
	{
		internal static string InternalApiServerUri => "http://localhost:8338/";

		internal abstract string CatCoreAuthServerUri { get; }

		internal abstract string TwitchClientId { get; }
	}
}