using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Requests
{
	internal readonly struct TopicNegotiationMessageData
	{
		public TopicNegotiationMessageData(string[] topics, string? token = null)
		{
			Token = token;
			Topics = topics;
		}

		[JsonPropertyName("topics")]
		public string[] Topics { get; }

		[JsonPropertyName("auth_token")]
		public string? Token { get; }
	}
}