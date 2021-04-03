using System.Text.Json.Serialization;

namespace CatCore.Models.Api.Requests
{
	internal readonly struct GlobalStateRequestDto
	{
		public bool LaunchWebAppOnStart { get; }
		public bool ParseEmojis { get; }

		[JsonConstructor]
		public GlobalStateRequestDto(bool launchWebAppOnStart, bool parseEmojis)
		{
			LaunchWebAppOnStart = launchWebAppOnStart;
			ParseEmojis = parseEmojis;
		}
	}
}