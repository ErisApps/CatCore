using System;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.EventSub
{
	internal struct EventSubMetadata
	{
		[JsonPropertyName("message_id")]
		public string MessageId { get; init; }

		[JsonPropertyName("message_type")]
		public string MessageType { get; init; }

		[JsonPropertyName("message_timestamp")]
		public DateTime MessageTimestamp { get; init; }

		[JsonPropertyName("subscription_type")]
		public string SubscriptionType { get; init; }

		[JsonPropertyName("subscription_version")]
		public string SubscriptionVersion { get; init; }
	}

	internal struct EventSubWebSocketMessage<TPayload> where TPayload : struct
	{
		[JsonPropertyName("metadata")]
		public EventSubMetadata Metadata { get; init; }

		[JsonPropertyName("payload")]
		public TPayload Payload { get; init; }
	}
}
