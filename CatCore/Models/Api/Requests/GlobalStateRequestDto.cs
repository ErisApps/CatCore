using System.Text.Json.Serialization;

namespace CatCore.Models.Api.Requests
{
	internal readonly struct GlobalStateRequestDto
	{
		public bool LaunchInternalApiOnStartup { get; }
		public bool LaunchWebPortalOnStartup { get; }
		public bool ParseEmojis { get; }

		[JsonConstructor]
		public GlobalStateRequestDto(bool launchInternalApiOnStartup, bool launchWebPortalOnStartup, bool parseEmojis)
		{
			LaunchInternalApiOnStartup = launchInternalApiOnStartup;
			LaunchWebPortalOnStartup = launchWebPortalOnStartup;
			ParseEmojis = parseEmojis;
		}
	}
}