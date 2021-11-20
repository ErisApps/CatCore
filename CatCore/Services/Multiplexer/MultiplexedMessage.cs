using System.Collections.ObjectModel;
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

		private readonly Info _info;
		private readonly object _message;

		internal object Underlying => _message;

		private MultiplexedMessage(Info info, object message)
			=> (_info, _message) = (info, message);

		public static MultiplexedMessage From<TMsg, TChannel>(TMsg message)
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
			=> new(Info<TChannel, TMsg>.INSTANCE, message);

		public string Id => _info.GetId(_message);

		public bool IsSystemMessage => _info.IsSystemMessage(_message);

		public bool IsActionMessage => _info.IsActionMessage(_message);

		public bool IsMentioned => _info.IsMentioned(_message);

		public string Message => _info.GetMessage(_message);

		public IChatUser Sender => _info.GetSender(_message);

		public MultiplexedChannel Channel => _info.GetChannel(_message);

		public ReadOnlyCollection<IChatEmote> Emotes => _info.GetEmotes(_message);

		public ReadOnlyDictionary<string, string>? Metadata => _info.GetMetadata(_message);
	}
}
