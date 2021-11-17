using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Models.Twitch.Helix.Responses.Emotes
{
	public class GlobalEmote
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("name")]
		public string Name { get; }

		[JsonPropertyName("images")]
		public DefaultImage Images { get; }

		[JsonPropertyName("format")]
		public IReadOnlyList<string> Format { get; }

		[JsonPropertyName("scale")]
		public IReadOnlyList<string> Scale { get; }

		[JsonPropertyName("theme_mode")]
		public IReadOnlyList<string> ThemeMode { get; }

		[JsonConstructor]
		public GlobalEmote(string id, string name, DefaultImage images, IReadOnlyList<string> format, IReadOnlyList<string> scale, IReadOnlyList<string> themeMode)
		{
			Id = id;
			Name = name;
			Images = images;
			Format = format;
			Scale = scale;
			ThemeMode = themeMode;
		}
	}
}