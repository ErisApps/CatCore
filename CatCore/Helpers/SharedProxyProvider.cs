#if !RELEASE
namespace CatCore.Helpers
{
	internal static class SharedProxyProvider
	{
		public static readonly System.Net.WebProxy? PROXY = null;
	}
}
#endif
