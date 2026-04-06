using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Exceptions;
using CatCore.Helpers.JSON;
using CatCore.Models.Twitch.Helix.Requests;
using CatCore.Models.Twitch.Helix.Responses;
using Serilog;

namespace CatCore.Services.Twitch
{
	public sealed partial class TwitchHelixApiService
	{
		/// <summary>
		/// Sends a chat message to a broadcaster's chat.
		/// </summary>
		/// <param name="broadcasterId">The ID of the broadcaster whose chat you want to send a message to</param>
		/// <param name="senderUserId">The ID of the user sending the message</param>
		/// <param name="message">The message text. Max length is 500 characters</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>True if the message was sent successfully, false otherwise</returns>
		/// <exception cref="TwitchNotAuthenticatedException">Gets thrown when the user isn't authenticated</exception>
		internal async Task<bool> SendChatMessage(string broadcasterId, string senderUserId, string message, CancellationToken cancellationToken = default)
		{
			await CheckUserLoggedIn().ConfigureAwait(false);

			if (message.Length > 500)
			{
				_logger.Warning("Message length exceeds 500 characters. Trimming to 500");
				message = message.Substring(0, 500);
			}

			var requestDto = new SendChatMessageRequestDto
			{
				BroadcasterId = broadcasterId,
				SenderId = senderUserId,
				Message = message
			};

			var url = $"chat/messages";
			try
			{
				var jsonContent = JsonSerializer.Serialize(requestDto, TwitchHelixSerializerContext.Default.SendChatMessageRequestDto);
				using var response = await _combinedHelixPolicy.ExecuteAsync(async ct =>
				{
					using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url)
					{
						Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
					};
					return await _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
				}, cancellationToken).ConfigureAwait(false);

				return response?.IsSuccessStatusCode ?? false;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to send chat message");
				return false;
			}
		}

		/// <summary>
		/// Creates an EventSub subscription for receiving real-time events.
		/// </summary>
		/// <param name="type">The subscription type (e.g., "channel.chat.message")</param>
		/// <param name="version">The subscription version (typically "1")</param>
		/// <param name="condition">Dictionary of condition parameters (e.g., {"broadcaster_user_id": "123"})</param>
		/// <param name="sessionId">The WebSocket session ID to receive events on</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>The subscription ID if successful, null otherwise</returns>
		internal async Task<string?> CreateEventSubSubscription(string type, string version, Dictionary<string, string> condition, string sessionId, CancellationToken cancellationToken = default)
		{
			if (!_twitchAuthService.HasTokens)
			{
				_logger.Warning("Token not valid. Either the user is not logged in or the token has been revoked");
				return null;
			}

			if (!_twitchAuthService.TokenIsValid && !await _twitchAuthService.RefreshTokens().ConfigureAwait(false))
			{
				return null;
			}

			var requestDto = new EventSubSubscriptionRequestDto
			{
				Type = type,
				Version = version,
				Condition = condition,
				Transport = new EventSubTransportDto
				{
					Method = "websocket",
					SessionId = sessionId
				}
			};

			var url = "eventsub/subscriptions";

#if DEBUG
			_logger.Verbose("Creating EventSub subscription: Type={Type}, Version={Version}, Condition={Condition}", type, version, string.Join(", ", condition.Select(kvp => $"{kvp.Key}={kvp.Value}")));
#endif

			try
			{
				using var httpResponseMessage = await _combinedHelixPolicy.ExecuteAsync(async ct =>
				{
					var jsonContent = JsonSerializer.Serialize(requestDto, TwitchHelixSerializerContext.Default.EventSubSubscriptionRequestDto);
					var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
					using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
					return await _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
				}, cancellationToken).ConfigureAwait(false);

				if (httpResponseMessage == null)
				{
					return null;
				}

				if (!httpResponseMessage.IsSuccessStatusCode)
				{
					var errorContent = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
					_logger.Warning("Failed to create EventSub subscription. Type={Type}, StatusCode={StatusCode}, Response={Response}", type, httpResponseMessage.StatusCode, errorContent);
					return null;
				}

				var responseJson = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
				var response = JsonSerializer.Deserialize(responseJson, TwitchHelixSerializerContext.Default.EventSubSubscriptionResponseDto);

				if (EqualityComparer<EventSubSubscriptionResponseDto>.Default.Equals(response, default))
				{
					_logger.Warning("Failed to deserialize EventSub subscription response. Type={Type}, Payload={Payload}", type, responseJson);
					return null;
				}

				if (response.Data != null && response.Data.Count > 0)
				{
					var subscriptionId = response.Data[0].Id;
					_logger.Information("EventSub subscription created successfully. ID: {SubscriptionId}", subscriptionId);
					return subscriptionId;
				}

				_logger.Warning("EventSub subscription response did not include data. Type={Type}, Payload={Payload}", type, responseJson);

				return null;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to create EventSub subscription");
				return null;
			}
		}

		/// <summary>
		/// Deletes an EventSub subscription.
		/// </summary>
		/// <param name="subscriptionId">The subscription ID to delete</param>
		/// <param name="cancellationToken">CancellationToken that can be used to cancel the call</param>
		/// <returns>True if deletion was successful, false otherwise</returns>
		internal async Task<bool> DeleteEventSubSubscription(string subscriptionId, CancellationToken cancellationToken = default)
		{
			if (!_twitchAuthService.HasTokens)
			{
				_logger.Warning("Token not valid. Either the user is not logged in or the token has been revoked");
				return false;
			}

			if (!_twitchAuthService.TokenIsValid && !await _twitchAuthService.RefreshTokens().ConfigureAwait(false))
			{
				return false;
			}

			var url = $"eventsub/subscriptions?id={Uri.EscapeDataString(subscriptionId)}";

#if DEBUG
			_logger.Verbose("Deleting EventSub subscription: {SubscriptionId}", subscriptionId);
#endif

			try
			{
				using var httpResponseMessage = await _combinedHelixPolicy.ExecuteAsync(async ct =>
				{
					using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, url);
					return await _helixClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
				}, cancellationToken).ConfigureAwait(false);

				if (httpResponseMessage == null)
				{
					_logger.Warning("Failed to delete EventSub subscription {SubscriptionId}: received null HTTP response", subscriptionId);
					return false;
				}

				if (!httpResponseMessage.IsSuccessStatusCode)
				{
					var errorContent = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
					_logger.Warning("Failed to delete EventSub subscription {SubscriptionId}. StatusCode={StatusCode}, Response={Response}", subscriptionId, httpResponseMessage.StatusCode, errorContent);
					return false;
				}

				_logger.Information("EventSub subscription deleted successfully. ID: {SubscriptionId}", subscriptionId);
				return true;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Failed to delete EventSub subscription {SubscriptionId}", subscriptionId);
				return false;
			}
		}
	}
}
