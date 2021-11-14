using System.Collections.ObjectModel;
using CatCore.Models.Shared;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.IRC
{
	public sealed class TwitchMessage : IChatMessage<TwitchMessage, TwitchChannel>
	{
		/// <inheritdoc />
		[PublicAPI]
		public string Id { get; internal set;  }

		/// <inheritdoc />
		[PublicAPI]
		public bool IsSystemMessage { get; internal set;  }

		/// <inheritdoc />
		[PublicAPI]
		public bool IsActionMessage { get; internal set; }

		/// <inheritdoc />
		[PublicAPI]
		public bool IsMentioned { get; internal set; }

		/// <inheritdoc />
		[PublicAPI]
		public string Message { get; internal set; }

		/// <inheritdoc />
		[PublicAPI]
		public IChatUser Sender { get; internal set; }

		/// <inheritdoc />
		[PublicAPI]
		public TwitchChannel Channel { get; internal set; }

		/// <inheritdoc />
		[PublicAPI]
		public ReadOnlyCollection<IChatEmote> Emotes { get; internal set; }

		/// <inheritdoc />
		[PublicAPI]
		public ReadOnlyDictionary<string, string>? Metadata { get; internal set; }

		/// <summary>
		/// The IRC message type for this TwitchMessage
		/// </summary>
		[PublicAPI]
		public string Type { get; internal set; }

		/// <summary>
		/// The number of bits in this message, if any.
		/// </summary>
		[PublicAPI]
		public uint Bits { get; internal set; }

		public TwitchMessage(string id, bool isSystemMessage, bool isActionMessage, bool isMentioned, string message, IChatUser sender, TwitchChannel channel, ReadOnlyCollection<IChatEmote> emotes,
			ReadOnlyDictionary<string, string>? metadata, string type, uint bits)
		{
			Id = id;
			IsSystemMessage = isSystemMessage;
			IsActionMessage = isActionMessage;
			IsMentioned = isMentioned;
			Message = message;
			Sender = sender;
			Channel = channel;
			Metadata = metadata;
			Emotes = emotes;
			Type = type;
			Bits = bits;
		}
	}
}