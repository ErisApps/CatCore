using System.Threading.Tasks;

namespace CatCore.Services.Twitch.Interfaces
{
	internal interface ITwitchIrcService
	{
		Task Start();
		Task Stop();
	}
}