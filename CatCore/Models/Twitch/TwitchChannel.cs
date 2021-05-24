using CatCore.Models.Shared;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch
{
	public class TwitchChannel : IChatChannel
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
	}
}