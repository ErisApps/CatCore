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

		event Action<string, ViewCountUpdate> OnViewCountUpdated;
		event Action<string, StreamUp> OnStreamUp;
		event Action<string, StreamDown> OnStreamDown;
		event Action<string, Commercial> OnCommercial;

		event Action<string, Follow> OnFollow;
		event Action<string, PollData> OnPoll;
		event Action<string, PredictionData> OnPrediction;
		event Action<string, RewardRedeemedData> OnRewardRedeemed;
	}
}