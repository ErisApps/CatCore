using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct ResponseBaseWithPagination<T>
	{
		[JsonPropertyName("data")]
		public List<T> Data { get; }

		[JsonPropertyName("pagination")]
		public Pagination Pagination { get; }

		[JsonConstructor]
		public ResponseBaseWithPagination(List<T> data, Pagination pagination)
		{
			Data = data;
			Pagination = pagination;
		}
	}
}