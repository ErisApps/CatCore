using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using CatCore.Helpers.JSON;
using CatCore.Models.Shared;
using CatCore.Models.ThirdParty.Bttv.Base;
using CatCore.Models.ThirdParty.Bttv.Ffz;
using Serilog;

namespace CatCore.Services.Twitch.Media
{
	// BTTV
	// https://api.betterttv.net/3/cached/emotes/global
	// https://api.betterttv.net/3/cached/users/twitch/${channel.id}
	//
	// FFZ
	// https://api.betterttv.net/3/cached/frankerfacez/emotes/global
	// https://api.betterttv.net/3/cached/frankerfacez/users/twitch/${currentChannel.id}
	//
	// Emote CDN format
	//http://cdn.betterttv.net/emote/${emoteId}/${version}
	public class BttvDataProvider
	{
		private const string BTTV_API_BASEURL = "https://api.betterttv.net/3/cached/";

		private readonly ILogger _logger;
		private readonly HttpClient _bttvApiClient;

		private IReadOnlyDictionary<string, ChatResourceData> _globalBttvData;
		private readonly Dictionary<string, IReadOnlyDictionary<string, ChatResourceData>> _channelBttvData;

		private IReadOnlyDictionary<string, ChatResourceData> _globalFfzData;
		private readonly Dictionary<string, IReadOnlyDictionary<string, ChatResourceData>> _channelFfzData;

		public BttvDataProvider(ILogger logger, Version libraryVersion)
		{
			_logger = logger;

			_bttvApiClient = new HttpClient
#if !RELEASE
				(new HttpClientHandler { Proxy = Helpers.SharedProxyProvider.PROXY })
#endif
				{
					BaseAddress = new Uri(BTTV_API_BASEURL, UriKind.Absolute)
				};
			_bttvApiClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{nameof(CatCore)}/{libraryVersion.ToString(3)}");

			_globalBttvData = new Dictionary<string, ChatResourceData>();
			_channelBttvData = new Dictionary<string, IReadOnlyDictionary<string, ChatResourceData>>();

			_globalFfzData = new Dictionary<string, ChatResourceData>();
			_channelFfzData = new Dictionary<string, IReadOnlyDictionary<string, ChatResourceData>>();
		}

		internal async Task<bool> TryRequestGlobalResources()
		{
			return (await Task.WhenAll(TryRequestGlobalBttvResources(), TryRequestGlobalFfzResources()).ConfigureAwait(false)).All(result => result);
		}

		internal async Task<bool> TryRequestChannelResources(string userId)
		{
			return (await Task.WhenAll(TryRequestBttvChannelResources(userId), TryRequestFfzChannelResources(userId)).ConfigureAwait(false)).All(result => result);
		}

		private async Task<bool> TryRequestGlobalBttvResources()
		{
			try
			{
				var (success, bttvGlobalData) = await GetAsync(BTTV_API_BASEURL + "emotes/global", BttvSerializerContext.Default.IReadOnlyListBttvEmote).ConfigureAwait(false);
				if (!success)
				{
					return false;
				}

				_globalBttvData = ParseBttvEmoteData(bttvGlobalData!, "BTTVGlobalEmote");

				return true;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the global BTTV emotes");

				return false;
			}
		}

		private async Task<bool> TryRequestBttvChannelResources(string userId)
		{
			try
			{
				var (success, bttvChannelData) = await GetAsync(BTTV_API_BASEURL + "users/twitch/" + userId, BttvSerializerContext.Default.BttvChannelData).ConfigureAwait(false);
				if (!success)
				{
					return false;
				}

				_channelBttvData[userId] = ParseBttvEmoteData(bttvChannelData.ChannelEmotes.Concat<EmoteBase>(bttvChannelData.SharedEmotes), "BTTVChannelEmote");

				return true;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the BTTV channel emotes for channel {ChannelId}", userId);

				return false;
			}
		}

		private async Task<bool> TryRequestGlobalFfzResources()
		{
			try
			{
				var (success, ffzGlobalData) =
					await GetAsync(BTTV_API_BASEURL + "frankerfacez/emotes/global", BttvSerializerContext.Default.IReadOnlyListFfzEmote).ConfigureAwait(false);
				if (!success)
				{
					return false;
				}

				_globalFfzData = ParseFfzEmoteData(ffzGlobalData!, "FFZGlobalEmote");

				return true;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the global FFZ emotes");

				return false;
			}
		}

		private async Task<bool> TryRequestFfzChannelResources(string userId)
		{
			try
			{
				var (success, ffzChannelData) =
					await GetAsync(BTTV_API_BASEURL + "frankerfacez/users/twitch/" + userId, BttvSerializerContext.Default.IReadOnlyListFfzEmote).ConfigureAwait(false);
				if (!success)
				{
					return false;
				}

				_channelFfzData[userId] = ParseFfzEmoteData(ffzChannelData!, "FFZChannelEmote");

				return true;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the FFZ channel emotes for channel {ChannelId}", userId);

				return false;
			}
		}

		private ReadOnlyDictionary<string, ChatResourceData> ParseBttvEmoteData(IEnumerable<EmoteBase> emoteData, string type)
		{
			var parsedEmotes = new Dictionary<string, ChatResourceData>();

			foreach (var emote in emoteData)
			{
				if (CheckIfAnimated(emote, out var isAnimated))
				{
					parsedEmotes.Add(emote.Code, new ChatResourceData(type + "_" + emote.Id, emote.Code, "https://cdn.betterttv.net/emote/" + emote.Id + "/3x", isAnimated, type));
				}
			}

			return new ReadOnlyDictionary<string, ChatResourceData>(parsedEmotes);
		}

		private ReadOnlyDictionary<string, ChatResourceData> ParseFfzEmoteData(IEnumerable<FfzEmote> emoteData, string type)
		{
			var parsedEmotes = new Dictionary<string, ChatResourceData>();

			foreach (var emote in emoteData)
			{
				var preferredUrl = emote.Images.PreferredUrl;
				if (preferredUrl == null)
				{
					_logger.Warning("No url found for FFZ emote {Code}", emote.Code);
					continue;
				}

				parsedEmotes.Add(emote.Code, new ChatResourceData(type + "_" + emote.Id, emote.Code, preferredUrl, false, type));
			}

			return new ReadOnlyDictionary<string, ChatResourceData>(parsedEmotes);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool CheckIfAnimated(EmoteBase emote, out bool isAnimated)
		{
			isAnimated = false;

			var imageType = emote.ImageType;
			if (imageType == "png")
			{
				return true;
			}

			if (imageType == "gif")
			{
				isAnimated = true;
				return true;
			}

			_logger.Warning("Unsupported imageType \"{ImageType}\" detected for emote {Code}", imageType, emote.Code);
			return false;
		}

		internal void ReleaseAllResources()
		{
			ReleaseBttvResources();
			ReleaseFfzResources();
		}

		internal void ReleaseBttvResources()
		{
			_globalBttvData = new Dictionary<string, ChatResourceData>();
			_channelBttvData.Clear();
		}

		internal void ReleaseFfzResources()
		{
			_globalFfzData = new Dictionary<string, ChatResourceData>();
			_channelFfzData.Clear();
		}

		internal void ReleaseChannelResources(string userId)
		{
			_channelBttvData.Remove(userId);
			_channelFfzData.Remove(userId);
		}

		public bool TryGetEmote(string identifier, string userId, out ChatResourceData? badge)
		{
			ChatResourceData badgeInternal;
			if (_channelBttvData.TryGetValue(userId, out var userSpecificBttvEmotes) && userSpecificBttvEmotes.TryGetValue(identifier, out badgeInternal) ||
			    _channelFfzData.TryGetValue(userId, out var userSpecificFfzEmotes) && userSpecificFfzEmotes.TryGetValue(identifier, out badgeInternal))
			{
				badge = badgeInternal;
				return true;
			}

			if (_globalBttvData.TryGetValue(identifier, out badgeInternal) || _globalFfzData.TryGetValue(identifier, out badgeInternal))
			{
				badge = badgeInternal;
				return true;
			}

			badge = null;
			return false;
		}

		private async Task<(bool success, TResponse? response)> GetAsync<TResponse>(string url, JsonTypeInfo<TResponse> jsonResponseTypeInfo, CancellationToken? cancellationToken = null)
		{
			try
			{
				using var httpResponseMessage = await _bttvApiClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
				if (!(httpResponseMessage?.IsSuccessStatusCode ?? false))
				{
					if (httpResponseMessage?.StatusCode == HttpStatusCode.NotFound)
					{
						_logger.Warning("No BetterTTV (proxied) emotes returned by endpoint {Url}", url);
					}

					return (false, default);
				}

				return (true, await httpResponseMessage.Content.ReadFromJsonAsync(jsonResponseTypeInfo, cancellationToken ?? CancellationToken.None).ConfigureAwait(false));
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to execute the GET call to {Uri}", url);
				return (false, default);
			}
		}
	}
}