using System.Net;

namespace CatCore.Helpers
{
	internal static class StatusCodeHelper
	{
		public const HttpStatusCode TOO_MANY_REQUESTS = (HttpStatusCode) 429;
	}
}