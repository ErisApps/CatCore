using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct ChannelData
	{
		[JsonPropertyName("broadcaster_login")]
		public string BroadcasterLogin { get; }

		[JsonPropertyName("broadcaster_language")]
		public string BroadcasterLanguage { get; }

		[JsonPropertyName("id")]
		public string ChannelId { get; }

		[JsonPropertyName("display_name")]
		public string DisplayName { get; }

		[JsonPropertyName("game_id")]
		public string GameId { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("thumbnail_url")]
		public string ThumbnailUrl { get; }

		[JsonPropertyName("is_live")]
		public bool IsLive { get; }

		[JsonPropertyName("started_at")]
		public string StartedAtRaw { get; }

		public DateTimeOffset? StartedAt => DateTimeOffset.TryParse(StartedAtRaw, out var parsedValue) ? parsedValue : null;

		[JsonPropertyName("tag_ids")]
		public List<string> TagIds { get; }

		[JsonConstructor]
		public ChannelData(string broadcasterLogin, string broadcasterLanguage, string channelId, string displayName, string gameId, string title, string thumbnailUrl, bool isLive,
			string startedAtRaw, List<string> tagIds)
		{
			BroadcasterLogin = broadcasterLogin;
			BroadcasterLanguage = broadcasterLanguage;
			ChannelId = channelId;
			DisplayName = displayName;
			GameId = gameId;
			Title = title;
			ThumbnailUrl = thumbnailUrl;
			IsLive = isLive;
			StartedAtRaw = startedAtRaw;
			TagIds = tagIds;
		}
	}
}