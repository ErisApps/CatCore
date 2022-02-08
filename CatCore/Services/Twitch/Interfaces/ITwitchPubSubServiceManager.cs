using System;
using System.Threading.Tasks;
using CatCore.Models.Twitch.PubSub.Responses;
using CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1;
using CatCore.Models.Twitch.PubSub.Responses.Polls;
using CatCore.Models.Twitch.PubSub.Responses.Predictions;
using CatCore.Models.Twitch.PubSub.Responses.VideoPlayback;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchPubSubServiceManager
	{
		internal Task Start();
		internal Task Stop();

		/// <summary>
		/// Fired whenever there's an update of the count of live viewers for a specific channel.
		/// First argument of the callback is the channelId on which the event was triggered.
		/// Second argument of the callback is additional data regarding the view count update.
		/// </summary>
		event Action<string, ViewCountUpdate> OnViewCountUpdated;

		/// <summary>
		/// Fired whenever a channel goes live.
		/// First argument of the callback is the channelId on which the event was triggered.
		/// Second argument of the callback is additional data regarding the StreamUp event.
		/// </summary>
		event Action<string, StreamUp> OnStreamUp;

		/// <summary>
		/// Fired whenever a channel stops streaming.
		/// First argument of the callback is the channelId on which the event was triggered.
		/// Second argument of the callback is additional data regarding the StreamDown event.
		/// </summary>
		event Action<string, StreamDown> OnStreamDown;

		/// <summary>
		/// Fired whenever a commercial is started on a specific channel.
		/// First argument of the callback is the channelId on which the event was triggered.
		/// Second argument of the callback is additional data regarding the OnCommercial event.
		/// </summary>
		event Action<string, Commercial> OnCommercial;

		/// <summary>
		/// Fired whenever a channel receives a new follower.
		/// First argument of the callback is the channelId on which the event was triggered.
		/// Second argument of the callback is additional data regarding the new follower.
		/// </summary>
		event Action<string, Follow> OnFollow;

		/// <summary>
		/// Fired whenever a channel starts a poll or when there's an update regarding an ongoing one.
		/// First argument of the callback is the channelId on which the event was triggered.
		/// Second argument of the callback is additional data regarding the poll.
		/// </summary>
		event Action<string, PollData> OnPoll;

		/// <summary>
		/// Fired whenever a channel starts a prediction or when there's an update regarding an ongoing one.
		/// First argument of the callback is the channelId on which the event was triggered.
		/// Second argument of the callback is additional data regarding the prediction.
		/// </summary>
		event Action<string, PredictionData> OnPrediction;

		/// <summary>
		/// Fired whenever a viewer redeems a reward on a specific channel.
		/// First argument of the callback is the channelId on which the event was triggered.
		/// Second argument of the callback is additional data regarding the redeemed reward and redeemer.
		/// </summary>
		event Action<string, RewardRedeemedData> OnRewardRedeemed;
	}
}