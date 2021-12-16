using CatCore.Models.Shared;

namespace CatCore.Models.Twitch.Media
{
	public class TwitchBadge : IChatBadge
	{
		public string Id { get; }
		public string Name { get; }
		public string Uri { get; }

		public TwitchBadge(string id, string name, string uri)
		{
			Id = id;
			Name = name;
			Uri = uri;
		}
	}
}