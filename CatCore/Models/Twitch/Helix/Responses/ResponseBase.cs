using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct ResponseBase<T>
	{
		[JsonPropertyName("data")]
		public List<T> Data { get; }

		[JsonConstructor]
		public ResponseBase(List<T> data)
		{
			Data = data;
		}
	}
}