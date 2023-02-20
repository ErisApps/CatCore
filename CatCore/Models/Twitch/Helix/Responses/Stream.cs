using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct Stream
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("user_login")]
		public string LoginName { get; }

		[JsonPropertyName("user_name")]
		public string DisplayName { get; }

		[JsonPropertyName("game_id")]
		public string GameId { get; }

		[JsonPropertyName("game_name")]
		public string GameName { get; }

		[JsonPropertyName("type")]
		public string Type { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("viewer_count")]
		public uint ViewerCount { get; }

		[JsonPropertyName("started_at")]
		public DateTimeOffset StartedAt { get; }

		[JsonPropertyName("language")]
		public string Language { get; }

		[JsonPropertyName("thumbnail_url")]
		public string ThumbnailUrl { get; }

		[JsonPropertyName("tag_ids")]
		[Obsolete("TagIds is deprecated by Twitch and will only contain an empty list. Use the Tags property instead which can contain custom tags. Will be removed in the next major version of CatCore")]
		public IReadOnlyList<string> TagIds { get; }

		[JsonPropertyName("tags")]
		public IReadOnlyList<string> Tags { get; }

		[JsonPropertyName("is_mature")]
		public bool IsMature { get; }

		[JsonConstructor]
		public Stream(string id, string userId, string loginName, string displayName, string gameId, string gameName, string type, string title, uint viewerCount, DateTimeOffset startedAt,
			string language, string thumbnailUrl, IReadOnlyList<string> tags, bool isMature)
		{
			Id = id;
			UserId = userId;
			LoginName = loginName;
			DisplayName = displayName;
			GameId = gameId;
			GameName = gameName;
			Type = type;
			Title = title;
			ViewerCount = viewerCount;
			StartedAt = startedAt;
			Language = language;
			ThumbnailUrl = thumbnailUrl;
#pragma warning disable CS0618
			TagIds = new List<string>(0);
#pragma warning restore CS0618
			Tags = tags;
			IsMature = isMature;
		}
	}
}