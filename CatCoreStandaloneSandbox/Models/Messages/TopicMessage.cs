using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCoreStandaloneSandbox.Models.Messages
{
    internal class TopicMessage : MessageBase
    {
        public TopicMessage(string type)
        {
            Type = type;
        }

        [JsonPropertyName("nonce")] public string? Nonce { get; init; }
        [JsonPropertyName("data")] public TopicMessageData Data { get; init; }

        internal class TopicMessageData
        {
            [JsonPropertyName("topics")] public List<string> Topics { get; init; }
            [JsonPropertyName("auth_token")] public string Token { get; init; }
        }
    }
}