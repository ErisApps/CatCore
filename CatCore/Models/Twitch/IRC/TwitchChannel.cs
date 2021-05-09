using CatCore.Models.Shared;

namespace CatCore.Models.Twitch.IRC
{
	public class TwitchChannel : IChatChannel
	{
		public string Id { get; }
		public string Name { get; }

		public TwitchChannel(string id, string name)
		{
			Id = id;
			Name = name;
		}
	}
}