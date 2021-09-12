using System;

namespace CatCore.Models.Credentials
{
	internal sealed class TwitchCredentials : ICredentials
	{
		public string? AccessToken { get; set; }
		public string? RefreshToken { get; set; }
		public DateTimeOffset? ValidUntil { get; set; }
	}
}