using CatCore.Models.Shared;

namespace CatCore.Services.Interfaces
{
	public interface IPlatformService<out TPlatform, TChannel, out TMessage>
		: IChatService<TPlatform, TChannel, TMessage>
		where TPlatform : IPlatformService<TPlatform, TChannel, TMessage>
		where TChannel : IChatChannel<TChannel, TMessage>
		where TMessage : IChatMessage<TMessage, TChannel>
	{
		internal void Start();
		internal void Stop();

		bool LoggedIn { get; }
		TChannel? DefaultChannel { get; }
	}
}