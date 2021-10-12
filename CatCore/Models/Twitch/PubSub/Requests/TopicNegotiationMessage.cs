using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Requests
{
	internal sealed class TopicNegotiationMessage : MessageBase
	{
		internal const string LISTEN = nameof(LISTEN);
		internal const string UNLISTEN = nameof(UNLISTEN);

		public TopicNegotiationMessage(string type, TopicNegotiationMessageData data, string nonce) : base(type)
		{
			Data = data;
			Nonce = nonce;
		}

		[JsonPropertyName("data")]
		public TopicNegotiationMessageData Data { get; }

		[JsonPropertyName("nonce")]
		public string Nonce { get; }
	}
}