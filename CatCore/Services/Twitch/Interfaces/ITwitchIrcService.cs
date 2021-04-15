using System.Threading.Tasks;
using CatCore.Services.Interfaces;

namespace CatCore.Services.Twitch.Interfaces
{
	public interface ITwitchIrcService : IChatService
	{
		internal Task Start();
		internal Task Stop();
	}
}