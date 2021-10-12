using System;
using System.Threading.Tasks;
using CatCore.Models.Twitch.PubSub.Responses;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchPubSubServiceManager
	{
		internal Task Start();
		internal Task Stop();

		event Action<string, Follow> OnFollow;
	}
}