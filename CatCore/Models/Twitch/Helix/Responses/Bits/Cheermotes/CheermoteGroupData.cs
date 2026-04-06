using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CatCore.Helpers.Converters;

namespace CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes
{
	public readonly struct CheermoteGroupData
	{
		[JsonPropertyName("prefix")]
		public string Prefix { get; }

		[JsonPropertyName("tiers")]
		public IReadOnlyList<CheermoteTier> Tiers { get; }

		[JsonPropertyName("type")]
		[JsonConverter(typeof(Helpers.Converters.JsonStringEnumConverter<CheermoteType>))]
		public CheermoteType Type { get; }

		[JsonPropertyName("order")]
		public int Order { get; }

		[JsonPropertyName("last_updated")]
		public DateTimeOffset LastUpdated { get; }

		[JsonPropertyName("is_charitable")]
		public bool IsCharitable { get; }

		[JsonConstructor]
		public CheermoteGroupData(string prefix, IReadOnlyList<CheermoteTier> tiers, CheermoteType type, int order, DateTimeOffset lastUpdated, bool isCharitable)
		{
			Prefix = prefix;
			Tiers = tiers;
			Type = type;
			Order = order;
			LastUpdated = lastUpdated;
			IsCharitable = isCharitable;
		}
	}
}