using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1
{
	public readonly struct GlobalCooldown
	{
		[JsonPropertyName("is_enabled")]
		public bool Enabled { get; }

		[JsonPropertyName("global_cooldown_seconds")]
		public uint CooldownSeconds { get; }

		[JsonConstructor]
		public GlobalCooldown(bool enabled, uint cooldownSeconds)
		{
			Enabled = enabled;
			CooldownSeconds = cooldownSeconds;
		}
	}
}