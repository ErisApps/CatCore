using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Requests
{
	internal readonly struct TopicNegotiationMessage
	{
		internal const string LISTEN = nameof(LISTEN);
		internal const string UNLISTEN = nameof(UNLISTEN);

		public TopicNegotiationMessage(string type, TopicNegotiationMessageData data, string nonce)
		{
			Type = type;
			Data = data;
			Nonce = nonce;
		}

		[JsonPropertyName("type")]
		public string Type { get; }

		[JsonPropertyName("data")]
		public TopicNegotiationMessageData Data { get; }

		[JsonPropertyName("nonce")]
		public string Nonce { get; }
	}
}