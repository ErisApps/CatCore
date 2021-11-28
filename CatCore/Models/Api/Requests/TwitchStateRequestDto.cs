using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Api.Requests
{
	internal readonly struct TwitchStateRequestDto
	{
		public bool SelfEnabled { get; }

		/// <remark>
		/// Key being the actual userId and the value being the loginname
		/// </remark>
		public Dictionary<string, string> AdditionalChannelsData { get; }

		public bool ParseBttvEmotes { get; }
		public bool ParseFfzEmotes { get; }
		public bool ParseTwitchEmotes { get; }
		public bool ParseCheermotes { get; }

		[JsonConstructor]
		public TwitchStateRequestDto(bool selfEnabled, Dictionary<string, string> additionalChannelsData, bool parseBttvEmotes, bool parseFfzEmotes, bool parseTwitchEmotes, bool parseCheermotes)
		{
			SelfEnabled = selfEnabled;
			AdditionalChannelsData = additionalChannelsData;
			ParseBttvEmotes = parseBttvEmotes;
			ParseFfzEmotes = parseFfzEmotes;
			ParseTwitchEmotes = parseTwitchEmotes;
			ParseCheermotes = parseCheermotes;
		}
	}
}