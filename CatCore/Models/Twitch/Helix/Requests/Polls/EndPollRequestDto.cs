using System.Text.Json.Serialization;
using CatCore.Helpers.Converters;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Models.Twitch.Helix.Requests.Polls
{
	internal readonly struct EndPollRequestDto
	{
		[JsonPropertyName("broadcaster_id")]
		public string BroadcasterId { get; }

		[JsonPropertyName("id")]
		public string PollId { get; }

		[JsonPropertyName("status")]
		[JsonConverter(typeof(Helpers.Converters.JsonStringEnumConverter<PollStatus>))]
		public PollStatus Status { get; }

		public EndPollRequestDto(string broadcasterId, string pollId, PollStatus status)
		{
			BroadcasterId = broadcasterId;
			PollId = pollId;
			Status = status;
		}
	}
}