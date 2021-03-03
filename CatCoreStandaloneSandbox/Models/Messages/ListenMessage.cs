using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCoreStandaloneSandbox.Models.Messages
{
    internal class ListenMessage : MessageBase
    {
        public ListenMessage()
        {
            Type = "LISTEN";
        }

        [JsonPropertyName("nonce")] public string? Nonce { get; init; }
        [JsonPropertyName("data")] public ListenMessageData Data { get; init; }

        internal class ListenMessageData
        {
            [JsonPropertyName("topics")] public List<string> Topics { get; init; }
            [JsonPropertyName("auth_token")] public string Token { get; init; }
        }
    }
}