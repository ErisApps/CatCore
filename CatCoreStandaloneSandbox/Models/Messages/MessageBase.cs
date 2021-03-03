using System.Text.Json.Serialization;

namespace CatCoreStandaloneSandbox.Models.Messages
{
    internal abstract class MessageBase
    {
        [JsonPropertyName("type")] public string Type { get; init; }
    }
}