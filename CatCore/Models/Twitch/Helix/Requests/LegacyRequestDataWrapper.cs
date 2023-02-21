using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Requests
{
	internal class LegacyRequestDataWrapper<T>
	{
		[JsonPropertyName("data")]
		public T Data { get; }

		[JsonConstructor]
		public LegacyRequestDataWrapper(T data)
		{
			Data = data;
		}
	}
}