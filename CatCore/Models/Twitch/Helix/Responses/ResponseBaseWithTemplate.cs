using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct ResponseBaseWithTemplate<T>
	{
		[JsonPropertyName("data")]
		public List<T> Data { get; }

		[JsonPropertyName("template")]
		public string Template { get; }

		[JsonConstructor]
		public ResponseBaseWithTemplate(List<T> data, string template)
		{
			Data = data;
			Template = template;
		}
	}
}