using CatCore.Models.Shared;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Twitch.Interfaces;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch
{
	public sealed class TwitchChannel : IChatChannel<TwitchChannel, TwitchMessage>
	{
		private readonly ITwitchIrcService _service;

		/// <inheritdoc />
		[PublicAPI]
		public string Id { get; }

		[PublicAPI]
		public string Name { get; }

		internal TwitchChannel(ITwitchIrcService service, string id, string name)
		{
			_service = service;

			Id = id;
			Name = name;
		}

		public object Clone()
		{
			return new TwitchChannel(_service, Id, Name);
		}

		public void SendMessage(string message) => _service.SendMessage(this, message);
	}
}