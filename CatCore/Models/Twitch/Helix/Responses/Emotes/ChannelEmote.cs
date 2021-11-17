using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Models.Twitch.Helix.Responses.Emotes
{
	public class ChannelEmote : GlobalEmote
	{
		[JsonPropertyName("tier")]
		public string Tier { get; }

		[JsonPropertyName("emote_type")]
		public string EmoteType { get; }

		[JsonPropertyName("emote_set_id")]
		public string EmoteSetId { get; }

		[JsonConstructor]
		public ChannelEmote(string id, string name, DefaultImage images, IReadOnlyList<string> format, IReadOnlyList<string> scale, IReadOnlyList<string> themeMode, string tier, string emoteType,
			string emoteSetId)
			: base(id, name, images, format, scale, themeMode)
		{
			Tier = tier;
			EmoteType = emoteType;
			EmoteSetId = emoteSetId;
		}
	}
}