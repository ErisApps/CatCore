using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.PubSub.Responses.Polls
{
	public readonly struct PollSettings
	{
		[JsonPropertyName("multi_choice")]
		public PollSettingsEntry MultiChoice { get; }

		[JsonPropertyName("subscriber_only")]
		public PollSettingsEntry SubscriberOnly { get; }

		[JsonPropertyName("subscriber_multiplier")]
		public PollSettingsEntry SubscriberMultiplier { get; }

		[JsonPropertyName("bits_votes")]
		public PollSettingsEntry BitsVotes { get; }

		[JsonPropertyName("channel_points_votes")]
		public PollSettingsEntry ChannelPointsVotes { get; }

		[JsonConstructor]
		public PollSettings(PollSettingsEntry multiChoice, PollSettingsEntry subscriberOnly, PollSettingsEntry subscriberMultiplier, PollSettingsEntry bitsVotes, PollSettingsEntry channelPointsVotes)
		{
			MultiChoice = multiChoice;
			SubscriberOnly = subscriberOnly;
			SubscriberMultiplier = subscriberMultiplier;
			BitsVotes = bitsVotes;
			ChannelPointsVotes = channelPointsVotes;
		}
	}
}