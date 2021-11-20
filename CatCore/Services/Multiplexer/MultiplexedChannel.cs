using CatCore.Models.Shared;

namespace CatCore.Services.Multiplexer
{
	public class MultiplexedChannel : IChatChannel<MultiplexedChannel, MultiplexedMessage>
	{
		private abstract class Info
		{
			public abstract string GetId(object o);
			public abstract string GetName(object o);
			public abstract void SendMessage(object o, string msg);
			public abstract object Clone(object o);
		}

		private class Info<TChannel, TMsg> : Info
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
		{
			public static readonly Info<TChannel, TMsg> INSTANCE = new();

			public override string GetId(object o) => ((TChannel) o).Id;
			public override string GetName(object o) => ((TChannel) o).Name;
			public override void SendMessage(object o, string msg) => ((TChannel) o).SendMessage(msg);
			public override object Clone(object o) => ((TChannel) o).Clone();
		}

		private readonly Info _info;
		private readonly object _channel;

		public object Underlying => _channel;

		private MultiplexedChannel(Info info, object channel)
			=> (_info, _channel) = (info, channel);

		public static MultiplexedChannel From<TChannel, TMsg>(TChannel channel)
			where TChannel : IChatChannel<TChannel, TMsg>
			where TMsg : IChatMessage<TMsg, TChannel>
			=> new(Info<TChannel, TMsg>.INSTANCE, channel);

		/// <inheritdoc />
		public string Id => _info.GetId(_channel);

		/// <inheritdoc />
		public string Name => _info.GetName(_channel);

		/// <inheritdoc />
		public void SendMessage(string message) => _info.SendMessage(_channel, message);

		public object Clone() => new MultiplexedChannel(_info, _info.Clone(_channel));
	}
}
