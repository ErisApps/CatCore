using System;
using System.Text.Json.Serialization;
using CatCore.Models.Twitch.Shared;

namespace CatCore.Models.Twitch.PubSub.Responses.ChannelPointsChannelV1
{
	public readonly struct Reward
	{
		[JsonPropertyName("id")]
		public string Id { get; }

		[JsonPropertyName("channel_id")]
		public string ChannelId { get; }

		[JsonPropertyName("title")]
		public string Title { get; }

		[JsonPropertyName("prompt")]
		public string Prompt { get; }

		[JsonPropertyName("cost")]
		public int Cost { get; }

		[JsonPropertyName("is_user_input_required")]
		public bool IsUserInputRequired { get; }

		[JsonPropertyName("is_sub_only")]
		public bool IsSubOnly { get; }

		[JsonPropertyName("image")]
		public object Image { get; }

		[JsonPropertyName("default_image")]
		public DefaultImage DefaultImage { get; }

		[JsonPropertyName("background_color")]
		public string BackgroundColor { get; }

		[JsonPropertyName("is_enabled")]
		public bool IsEnabled { get; }

		[JsonPropertyName("is_paused")]
		public bool IsPaused { get; }

		[JsonPropertyName("is_in_stock")]
		public bool IsInStock { get; }

		[JsonPropertyName("max_per_stream")]
		public MaxPerStream MaxPerStream { get; }

		[JsonPropertyName("should_redemptions_skip_request_queue")]
		public bool ShouldRedemptionsSkipRequestQueue { get; }

		/// <remarks>
		/// return type unsure
		/// </remarks>>
		[JsonPropertyName("template_id")]
		public string TemplateId { get; }

		[JsonPropertyName("updated_for_indicator_at")]
		public DateTimeOffset UpdatedForIndicatorAt { get; }

		[JsonPropertyName("max_per_user_per_stream")]
		public MaxPerUserPerStream MaxPerUserPerStream { get; }

		[JsonPropertyName("global_cooldown")]
		public GlobalCooldown GlobalCooldown { get; }

		/// <remarks>
		/// TODO: Figure out return type
		/// </remarks>>
		/*[JsonPropertyName("redemptions_redeemed_current_stream")]
		public object RedemptionsRedeemedCurrentStream { get; }*/

		[JsonPropertyName("cooldown_expires_at")]
		public string? CooldownExpiresAtRaw { get; }

		[JsonIgnore]
		public DateTimeOffset? CooldownExpiresAt => DateTimeOffset.TryParse(CooldownExpiresAtRaw, out var parsedValue) ? parsedValue : null;

		[JsonConstructor]
		public Reward(string id, string channelId, string title, string prompt, int cost, bool isUserInputRequired, bool isSubOnly, object image, DefaultImage defaultImage, string backgroundColor,
			bool isEnabled, bool isPaused, bool isInStock, MaxPerStream maxPerStream, bool shouldRedemptionsSkipRequestQueue, string templateId, DateTimeOffset updatedForIndicatorAt,
			MaxPerUserPerStream maxPerUserPerStream, GlobalCooldown globalCooldown, /*object redemptionsRedeemedCurrentStream,*/ string? cooldownExpiresAtRaw)
		{
			Id = id;
			ChannelId = channelId;
			Title = title;
			Prompt = prompt;
			Cost = cost;
			IsUserInputRequired = isUserInputRequired;
			IsSubOnly = isSubOnly;
			Image = image;
			DefaultImage = defaultImage;
			BackgroundColor = backgroundColor;
			IsEnabled = isEnabled;
			IsPaused = isPaused;
			IsInStock = isInStock;
			MaxPerStream = maxPerStream;
			ShouldRedemptionsSkipRequestQueue = shouldRedemptionsSkipRequestQueue;
			TemplateId = templateId;
			UpdatedForIndicatorAt = updatedForIndicatorAt;
			MaxPerUserPerStream = maxPerUserPerStream;
			GlobalCooldown = globalCooldown;
			/*
			RedemptionsRedeemedCurrentStream = redemptionsRedeemedCurrentStream;
			*/
			CooldownExpiresAtRaw = cooldownExpiresAtRaw;
		}
	}
}