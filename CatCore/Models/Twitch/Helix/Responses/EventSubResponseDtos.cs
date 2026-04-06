using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CatCore.Models.Twitch.Helix.Responses
{
	[PublicAPI]
	internal readonly struct EventSubSubscriptionResponseDto
	{
		[JsonConstructor]
		public EventSubSubscriptionResponseDto(
			List<EventSubSubscriptionInfoDto> data,
			int total,
			int totalCost,
			int maxTotalCost,
			Dictionary<string, string>? pagination)
		{
			Data = data ?? new List<EventSubSubscriptionInfoDto>();
			Total = total;
			TotalCost = totalCost;
			MaxTotalCost = maxTotalCost;
			Pagination = pagination;
		}

		[JsonPropertyName("data")]
		public List<EventSubSubscriptionInfoDto> Data { get; }

		[JsonPropertyName("total")]
		public int Total { get; }

		[JsonPropertyName("total_cost")]
		public int TotalCost { get; }

		[JsonPropertyName("max_total_cost")]
		public int MaxTotalCost { get; }

		[JsonPropertyName("pagination")]
		public Dictionary<string, string>? Pagination { get; }
	}

	[PublicAPI]
	internal readonly struct EventSubSubscriptionInfoDto
	{
		[JsonConstructor]
		public EventSubSubscriptionInfoDto(
			string id,
			string type,
			string version,
			string status,
			Dictionary<string, string> condition,
			Dictionary<string, string> transport,
			DateTime createdAt,
			int cost)
		{
			Id = id;
			Type = type;
			Version = version;
			Status = status;
			Condition = condition ?? new Dictionary<string, string>();
			Transport = transport ?? new Dictionary<string, string>();
			CreatedAt = createdAt;
			Cost = cost;
		}

		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("type")]
		public string Type { get; }

		[JsonPropertyName("version")]
		public string Version { get; }

		[JsonPropertyName("status")]
		public string Status { get; }

		[JsonPropertyName("condition")]
		public Dictionary<string, string> Condition { get; }

		[JsonPropertyName("transport")]
		public Dictionary<string, string> Transport { get; }

		[JsonPropertyName("created_at")]
		public DateTime CreatedAt { get; }

		[JsonPropertyName("cost")]
		public int Cost { get; }
	}
}
