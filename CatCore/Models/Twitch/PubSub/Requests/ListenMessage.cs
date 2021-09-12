using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Requests
{
	internal sealed class ListenMessage : MessageBase
	{
		[JsonConstructor]
		public ListenMessage(string nonce, ListenMessageData listenMessageData) : base("LISTEN")
		{
			Nonce = nonce;
			Data = listenMessageData;
		}

		[JsonPropertyName("nonce")]
		public string Nonce { get; }

		[JsonPropertyName("data")]
		public ListenMessageData Data { get; }

		internal sealed class ListenMessageData
		{
			[JsonPropertyName("auth_token")]
			public string Token { get; }

			[JsonPropertyName("topics")]
			public string[] Topics { get; }

			[JsonConstructor]
			public ListenMessageData( string token, params string[] topics)
			{
				Token = token;
				Topics = topics;
			}
		}
	}
}