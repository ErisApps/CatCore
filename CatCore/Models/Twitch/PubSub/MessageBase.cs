using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub
{
	public class MessageBase
	{
		[JsonPropertyName("type")]
		public string Type { get; }

		internal MessageBase(string type)
		{
			Type = type;
		}
	}
}