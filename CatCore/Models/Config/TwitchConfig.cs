using System.Collections.Generic;

namespace CatCore.Models.Config
{
	internal sealed class TwitchConfig
	{
		public bool OwnChannelEnabled { get; set; } = true;

		public Dictionary<string, string> AdditionalChannelsData { get; set; } = new Dictionary<string, string>();

		public bool ParseBttvEmotes { get; set; } = true;
		public bool ParseFfzEmotes { get; set; } = true;
		public bool ParseTwitchEmotes { get; set; } = true;
		public bool ParseCheermotes { get; set; } = true;
	}
}