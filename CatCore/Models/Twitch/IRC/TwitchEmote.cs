using CatCore.Models.Shared;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.IRC
{
	public sealed class TwitchEmote : IChatEmote
	{
		[PublicAPI]
		public string Id { get; internal set; }

		public string Name { get; internal set; }

		public TwitchEmote(string id, string name)
		{
			Id = id;
			Name = name;
		}
	}
}