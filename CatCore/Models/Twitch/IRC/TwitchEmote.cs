using CatCore.Models.Shared;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.IRC
{
	public class TwitchEmote : IChatEmote
	{
		[PublicAPI]
		public string ID { get; internal set; }

		public string Name { get; internal set; }

		public TwitchEmote(string id, string name)
		{
			ID = id;
			Name = name;
		}
	}
}