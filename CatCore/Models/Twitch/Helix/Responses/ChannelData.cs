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

		[JsonIgnore]
		public DateTimeOffset? StartedAt => DateTimeOffset.TryParse(StartedAtRaw, out var parsedValue) ? parsedValue : null;

		[JsonPropertyName("tag_ids")]
		[Obsolete("TagIds is deprecated by Twitch and will only contain an empty list. Use the Tags property instead which can contain custom tags. Will be removed in the next major version of CatCore")]
		public List<string> TagIds { get; }

		[JsonPropertyName("tags")]
		public List<string> Tags { get; }

		[JsonConstructor]
		public ChannelData(string broadcasterLogin, string broadcasterLanguage, string channelId, string displayName, string gameId, string title, string thumbnailUrl, bool isLive,
			string startedAtRaw, List<string> tags)
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
			Tags = tags;
#pragma warning disable CS0618 // Type or member is obsolete
			TagIds = new List<string>(0);
#pragma warning restore CS0618 // Type or member is obsolete
		}
	}
}