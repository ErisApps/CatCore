using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CatCore.Models.Twitch.Helix.Responses
{
	public readonly struct FollowedStream
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("user_id")]
		public string UserId { get; }

		[JsonPropertyName("user_login")]
		public string UserLogin { get; }

		[JsonPropertyName("user_name")]
		public string UserName { get; }

		[JsonPropertyName("game_id")]
		public string GameId { get; }

		[JsonPropertyName("game_name")]
		public string GameName { get; }

		[JsonPropertyName("type")]
		public string Type { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("viewer_count")]
		public int ViewerCount { get; }

		[JsonPropertyName("started_at")]
		public DateTimeOffset StartedAt { get; }

		[JsonPropertyName("language")]
		public string Language { get; }

		[JsonPropertyName("thumbnail_url")]
		public string ThumbnailUrl { get; }

		[JsonPropertyName("tag_ids")]
		public IReadOnlyList<string> TagIds { get; }

		[JsonConstructor]
		public FollowedStream(string id, string userId, string userLogin, string userName, string gameId, string gameName, string type, string title, int viewerCount, DateTime startedAt,
			string language, string thumbnailUrl, IReadOnlyList<string> tagIds)
		{
			Id = id;
			UserId = userId;
			UserLogin = userLogin;
			UserName = userName;
			GameId = gameId;
			GameName = gameName;
			Type = type;
			Title = title;
			ViewerCount = viewerCount;
			StartedAt = startedAt;
			Language = language;
			ThumbnailUrl = thumbnailUrl;
			TagIds = tagIds;
		}
	}
}