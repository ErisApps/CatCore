using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.Helix.Requests
{
	[PublicAPI]
	internal struct SendChatMessageRequestDto
	{
		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; init; }

		[JsonPropertyName("sender_id")]
		public string SenderId { get; init; }

		[JsonPropertyName("message")]
		public string Message { get; init; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		[JsonPropertyName("reply_parent_message_id")]
		public string? ReplyParentMessageId { get; init; }
	}

	[PublicAPI]
	internal struct EventSubSubscriptionRequestDto
	{
		[JsonPropertyName("type")]
		public string Type { get; init; }

		[JsonPropertyName("version")]
		public string Version { get; init; }

		[JsonPropertyName("condition")]
		public Dictionary<string, string> Condition { get; init; }

		[JsonPropertyName("transport")]
		public EventSubTransportDto Transport { get; init; }
	}

	[PublicAPI]
	internal struct EventSubTransportDto
	{
		[JsonPropertyName("method")]
		public string Method { get; init; }

		[JsonPropertyName("session_id")]
		public string SessionId { get; init; }
	}
}

