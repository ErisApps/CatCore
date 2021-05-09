using System.Collections.ObjectModel;
using CatCore.Models.Shared;

namespace CatCore.Models.Twitch.IRC
{
	public class TwitchMessage : IChatMessage
	{
		/// <inheritdoc cref="IChatMessage.Id"/>
		public string Id { get; }

		/// <inheritdoc cref="IChatMessage.IsSystemMessage"/>
		public bool IsSystemMessage { get; }

		/// <inheritdoc cref="IChatMessage.IsActionMessage"/>
		public bool IsActionMessage { get; }

		/// <inheritdoc cref="IChatMessage.IsHighlighted"/>
		public bool IsHighlighted { get; }

		/// <inheritdoc cref="IChatMessage.IsPing"/>
		public bool IsPing { get; }

		/// <inheritdoc cref="IChatMessage.Message"/>
		public string Message { get; }

		/// <inheritdoc cref="IChatMessage.Sender"/>
		public IChatUser Sender { get; }

		/// <inheritdoc cref="IChatMessage.Channel"/>
		public IChatChannel Channel { get; }

		/// <inheritdoc cref="IChatMessage.Metadata"/>
		public ReadOnlyDictionary<string, string> Metadata { get; }

		/// <summary>
		/// The IRC message type for this TwitchMessage
		/// </summary>
		public string Type { get; internal set; }

		/// <summary>
		/// The number of bits in this message, if any.
		/// </summary>
		public int Bits { get; internal set; }

		public TwitchMessage(string id, bool isSystemMessage, bool isActionMessage, bool isHighlighted, bool isPing, string message, IChatUser sender, IChatChannel channel,
			ReadOnlyDictionary<string, string> metadata, string type, int bits)
		{
			Id = id;
			IsSystemMessage = isSystemMessage;
			IsActionMessage = isActionMessage;
			IsHighlighted = isHighlighted;
			IsPing = isPing;
			Message = message;
			Sender = sender;
			Channel = channel;
			Metadata = metadata;
			Type = type;
			Bits = bits;
		}
	}
}