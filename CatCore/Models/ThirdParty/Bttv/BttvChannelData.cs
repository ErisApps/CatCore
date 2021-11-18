using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.ThirdParty.Bttv
{
	public readonly struct BttvChannelData
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		// TODO: Investigate what this would imply...
		/*[JsonPropertyName("bots")]
		public IReadOnlyList<object> Bots { get; }*/

		[JsonPropertyName("avatar")]
		public string Avatar { get; }

		[JsonPropertyName("channelEmotes")]
		public IReadOnlyList<BttvEmote> ChannelEmotes { get; }

		[JsonPropertyName("sharedEmotes")]
		public IReadOnlyList<BttvSharedEmote> SharedEmotes { get; }

		[JsonConstructor]
		public BttvChannelData(string id, string avatar, IReadOnlyList<BttvEmote> channelEmotes, IReadOnlyList<BttvSharedEmote> sharedEmotes)
		{
			Id = id;
			Avatar = avatar;
			ChannelEmotes = channelEmotes;
			SharedEmotes = sharedEmotes;
		}
	}
}