using System;

namespace CatCore.Models.Shared
{
	public interface IChatChannel<out TChannel, out TMessage> : ICloneable
		where TChannel : IChatChannel<TChannel, TMessage>
		where TMessage : IChatMessage<TMessage, TChannel>
	{
		string Id { get; }
		string Name { get; }
	}
}