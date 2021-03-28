using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct Pagination
	{
		[JsonPropertyName("cursor")]
		public string Cursor { get; }

		[JsonConstructor]
		public Pagination(string cursor)
		{
			Cursor = cursor;
		}
	}
}