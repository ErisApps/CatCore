using CatCore.Models.Shared;

namespace CatCore.Models.Twitch
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