using System;

namespace CatCore.Models.Shared
{
	public interface IChatChannel<out TChannel, out TMessage> : ICloneable
		where TChannel : IChatChannel<TChannel, TMessage>
		where TMessage : IChatMessage<TMessage, TChannel>
	{
		/// <summary>
		/// The id of the channel
		/// </summary>
		string Id { get; }

		/// <summary>
		/// The name of the channel
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Sends a message to channel that this instance represents
		/// </summary>
		/// <param name="message">The actual message that will be send to said channel</param>
		void SendMessage(string message);
	}
}