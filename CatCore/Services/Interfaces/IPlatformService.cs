using System.Threading.Tasks;
using CatCore.Models.Shared;

namespace CatCore.Services.Interfaces
{
	public interface IPlatformService<out TPlatform, out TChannel, out TMessage>
		: IChatService<TPlatform, TChannel, TMessage>
		where TPlatform : IPlatformService<TPlatform, TChannel, TMessage>
		where TChannel : IChatChannel<TChannel, TMessage>
		where TMessage : IChatMessage<TMessage, TChannel>
	{
		internal Task Start();
		internal Task Stop();

		/// <summary>
		/// Indicates whether the user is authenticated for this service
		/// </summary>
		bool LoggedIn { get; }

		/// <summary>
		/// Returns the default channel for this service
		/// </summary>
		TChannel? DefaultChannel { get; }
	}
}