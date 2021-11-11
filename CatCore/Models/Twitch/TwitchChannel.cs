using CatCore.Models.Shared;
using CatCore.Models.Twitch.IRC;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch
{
	public sealed class TwitchChannel : IChatChannel<TwitchChannel, TwitchMessage>
	{
		[PublicAPI]
		public string Id { get; }

		[PublicAPI]
		public string Name { get; }

		public TwitchChannel(string id, string name)
		{
			Id = id;
			Name = name;
		}

		public object Clone()
		{
			return new TwitchChannel(Id, Name);
		}
	}
}