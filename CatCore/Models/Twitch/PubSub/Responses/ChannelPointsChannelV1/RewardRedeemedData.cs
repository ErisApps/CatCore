using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1
{
	public readonly struct RewardRedeemedData
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("user")]
		public User User { get; }

		[JsonPropertyName("channel_id")]
		public string ChannelId { get; }

		[JsonPropertyName("redeemed_at")]
		public string RedeemedAt { get; }

		[JsonPropertyName("reward")]
		public Reward Reward { get; }

		[JsonPropertyName("status")]
		public string Status { get; }

		[JsonConstructor]
		public RewardRedeemedData(string id, User user, string channelId, string redeemedAt, Reward reward, string status)
		{
			Id = id;
			User = user;
			ChannelId = channelId;
			RedeemedAt = redeemedAt;
			Reward = reward;
			Status = status;
		}
	}
}