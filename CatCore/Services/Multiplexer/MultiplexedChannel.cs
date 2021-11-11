using CatCore.Models.Shared;

namespace CatCore.Services.Multiplexer
{
	public class MultiplexedChannel : IChatChannel<MultiplexedChannel, MultiplexedMessage>
	{
		private abstract class Info
		{
			public abstract string GetId(object o);
			public abstract string GetName(object o);
			public abstract object Clone(object o);
		}

		private class Info<TChannel, TMsg> : Info
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
		{
			public static readonly Info<TChannel, TMsg> INSTANCE = new();

			public override string GetId(object o) => ((TChannel) o).Id;
			public override string GetName(object o) => ((TChannel) o).Name;
			public override object Clone(object o) => ((TChannel) o).Clone();
		}

		private readonly Info info;
		private readonly object channel;

		private MultiplexedChannel(Info info, object channel)
			=> (this.info, this.channel) = (info, channel);

		public static MultiplexedChannel From<TChannel, TMsg>(TChannel channel)
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
			=> new(Info<TChannel, TMsg>.INSTANCE, channel);

		public string Id => info.GetId(channel);

		public string Name => info.GetName(channel);

		public object Clone() => new MultiplexedChannel(info, info.Clone(channel));
	}
}
 