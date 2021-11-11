using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using CatCore.Models.Shared;

namespace CatCore.Services.Multiplexer
{
	public class MultiplexedMessage : IChatMessage<MultiplexedMessage, MultiplexedChannel>
	{
		private abstract class Info
		{
			public abstract string GetId(object o);
			public abstract bool IsSystemMessage(object o);
			public abstract bool IsActionMessage(object o);
			public abstract bool IsMentioned(object o);
			public abstract string GetMessage(object o);
			public abstract IChatUser GetSender(object o);
			public abstract MultiplexedChannel GetChannel(object o);
			public abstract ReadOnlyCollection<IChatEmote> GetEmotes(object o);
			public abstract ReadOnlyDictionary<string, string>? GetMetadata(object o);
		}

		private class Info<TChannel, TMsg> : Info
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
		{
			public static readonly Info<TChannel, TMsg> INSTANCE = new();

			public override string GetId(object o) => ((TMsg) o).Id;
			public override bool IsSystemMessage(object o) => ((TMsg) o).IsSystemMessage;
			public override bool IsActionMessage(object o) => ((TMsg) o).IsActionMessage;
			public override bool IsMentioned(object o) => ((TMsg) o).IsMentioned;
			public override string GetMessage(object o) => ((TMsg) o).Message;
			public override IChatUser GetSender(object o) => ((TMsg) o).Sender;
			public override MultiplexedChannel GetChannel(object o) => MultiplexedChannel.From<TChannel, TMsg>(((TMsg) o).Channel);
			public override ReadOnlyCollection<IChatEmote> GetEmotes(object o) => ((TMsg) o).Emotes;
			public override ReadOnlyDictionary<string, string>? GetMetadata(object o) => ((TMsg) o).Metadata;
		}

		private readonly Info info;
		private readonly object message;

		private MultiplexedMessage(Info info, object message)
			=> (this.info, this.message) = (info, message);

		public static MultiplexedMessage From<TMsg, TChannel>(TMsg message)
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
			=> new(Info<TChannel, TMsg>.INSTANCE, message);

		public string Id => info.GetId(message);

		public bool IsSystemMessage => info.IsSystemMessage(message);

		public bool IsActionMessage => info.IsActionMessage(message);

		public bool IsMentioned => info.IsMentioned(message);

		public string Message => info.GetMessage(message);

		public IChatUser Sender => info.GetSender(message);

		public MultiplexedChannel Channel => info.GetChannel(message);

		public ReadOnlyCollection<IChatEmote> Emotes => info.GetEmotes(message);

		public ReadOnlyDictionary<string, string>? Metadata => info.GetMetadata(message);
	}
}
