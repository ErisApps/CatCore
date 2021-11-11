using CatCore.Models.Shared;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Twitch.Interfaces;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch
{
	public sealed class TwitchChannel : IChatChannel<TwitchChannel, TwitchMessage>
	{
		private readonly ITwitchIrcService service;

		[PublicAPI]
		public string Id { get; }

		[PublicAPI]
		public string Name { get; }

		internal TwitchChannel(ITwitchIrcService service, string id, string name)
		{
			this.service = service;
			Id = id;
			Name = name;
		}

		public object Clone()
		{
			return new TwitchChannel(service, Id, Name);
		}

		public void SendMessage(string message) => service.SendMessage(this, message);
	}
}