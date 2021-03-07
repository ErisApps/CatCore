using CatCore.Models.Credentials;
using CatCore.Services.Interfaces;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchCredentialsProvider : IKittenCredentialsProvider<TwitchCredentials>
	{
	}
}