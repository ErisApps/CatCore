using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Predictions
{
	public readonly struct Predictor
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("user_login")]
		public string LoginName { get; }

		[JsonPropertyName("user_name")]
		public string DisplayName { get; }

		[JsonPropertyName("channel_points_used")]
		public uint ChannelPointsUsed { get; }

		[JsonPropertyName("channel_points_won")]
		public uint ChannelPointsWon { get; }

		[JsonConstructor]
		public Predictor(string userId, string loginName, string displayName, uint channelPointsUsed, uint channelPointsWon)
		{
			UserId = userId;
			LoginName = loginName;
			DisplayName = displayName;
			ChannelPointsUsed = channelPointsUsed;
			ChannelPointsWon = channelPointsWon;
		}
	}
}