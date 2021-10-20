using System;
using System.Threading.Tasks;
using CatCore.Models.Twitch.PubSub.Responses;
using CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1;
using CatCore.Models.Twitch.PubSub.Responses.Polls;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchPubSubServiceManager
	{
		internal Task Start();
		internal Task Stop();

		event Action<string, Follow> OnFollow;
		event Action<string, PollData> OnPoll;
		event Action<string, RewardRedeemedData> OnRewardRedeemed;
	}
}