using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Credentials
{
	internal class TwitchCredentials : ICredentials
	{
		public string? AccessToken { get; set; }
		public string? RefreshToken { get; set; }
		[JsonIgnore]
		public DateTimeOffset? ValidUntil { get; set; }
	}
}