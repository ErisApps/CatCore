using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses.Predictions
{
	public readonly struct Predictor
	{
		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; }

		[JsonPropertyName("user_name")]
		public string UserName { get; }

		[JsonPropertyName("channel_points_used")]
		public uint ChannelPointsUsed { get; }

		[JsonPropertyName("channel_points_won")]
		public uint ChannelPointsWon { get; }

		[JsonConstructor]
		public Predictor(string userId, string userLogin, string userName, uint channelPointsUsed, uint channelPointsWon)
		{
			UserId = userId;
			UserLogin = userLogin;
			UserName = userName;
			ChannelPointsUsed = channelPointsUsed;
			ChannelPointsWon = channelPointsWon;
		}
	}
}