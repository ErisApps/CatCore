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
using CatCore.Models.ThirdParty.Bttv;
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
	// BTTV Emote CDN format (version is either 1x, 2x or 3x)
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

		internal async Task TryRequestGlobalBttvResources()
		{
			try
			{
				var (success, bttvGlobalData) = await GetAsync(BTTV_API_BASEURL + "emotes/global", BttvSerializerContext.Default.IReadOnlyListBttvEmote).ConfigureAwait(false);
				if (!success)
				{
					_logger.Warning("Something went wrong while trying to fetch the global BTTV emotes, the call was not successful");
					return;
				}

				_globalBttvData = ParseBttvEmoteData(bttvGlobalData!, "BTTVGlobalEmote");

				_logger.Debug("Finished caching {EmoteCount} global BTTV emotes", _globalBttvData.Count);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the global BTTV emotes");
			}
		}

		internal async Task TryRequestBttvChannelResources(string userId)
		{
			try
			{
				var (success, bttvChannelData) = await GetAsync(BTTV_API_BASEURL + "users/twitch/" + userId, BttvSerializerContext.Default.BttvChannelData).ConfigureAwait(false);
				if (!success)
				{
					_logger.Warning("Something went wrong while trying to fetch the BTTV channel emotes for channel {ChannelId}, the call was not successful", userId);
					return;
				}

				_channelBttvData[userId] = ParseBttvEmoteData(bttvChannelData.ChannelEmotes.Concat<BttvEmoteBase>(bttvChannelData.SharedEmotes), "BTTVChannelEmote");

				_logger.Debug("Finished caching {EmoteCount} BTTV channel emotes for channel {ChannelId}", _channelBttvData[userId].Count, userId);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the BTTV channel emotes for channel {ChannelId}", userId);
			}
		}

		internal async Task TryRequestGlobalFfzResources()
		{
			try
			{
				var (success, ffzGlobalData) =
					await GetAsync(BTTV_API_BASEURL + "frankerfacez/emotes/global", BttvSerializerContext.Default.IReadOnlyListFfzEmote).ConfigureAwait(false);
				if (!success)
				{
					_logger.Warning("Something went wrong while trying to fetch the global FFZ emotes, the call was not successful");
					return;
				}

				if (ffzGlobalData!.Any())
				{
					_globalFfzData = ParseFfzEmoteData(ffzGlobalData!, "FFZGlobalEmote");
				}

				_logger.Debug("Finished caching {EmoteCount} global FFZ emotes", _globalFfzData.Count);
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the global FFZ emotes");
			}
		}

		internal async Task TryRequestFfzChannelResources(string userId)
		{
			try
			{
				var (success, ffzChannelData) =
					await GetAsync(BTTV_API_BASEURL + "frankerfacez/users/twitch/" + userId, BttvSerializerContext.Default.IReadOnlyListFfzEmote).ConfigureAwait(false);
				if (!success)
				{
					_logger.Warning("Something went wrong while trying to fetch the FFZ channel emotes for channel {ChannelId}, the call was not successful", userId);
					return;
				}

				if (ffzChannelData!.Any())
				{
					_channelFfzData[userId] = ParseFfzEmoteData(ffzChannelData!, "FFZChannelEmote");

					_logger.Debug("Finished caching {EmoteCount} FFZ channel emotes for channel {ChannelId}", _channelFfzData[userId].Count, userId);
				}
				else
				{
					_logger.Debug("No FFZ channel emotes found for channel {ChannelId}", userId);
				}
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the FFZ channel emotes for channel {ChannelId}", userId);
			}
		}

		private ReadOnlyDictionary<string, ChatResourceData> ParseBttvEmoteData(IEnumerable<BttvEmoteBase> emoteData, string type)
		{
			var parsedEmotes = new Dictionary<string, ChatResourceData>();

			foreach (var emote in emoteData)
			{
				if (CheckIfAnimated(emote, out var isAnimated))
				{
					parsedEmotes[emote.Code] = new ChatResourceData(type + "_" + emote.Id, emote.Code, "https://cdn.betterttv.net/emote/" + emote.Id + "/3x", isAnimated, type);
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

				parsedEmotes[emote.Code] = new ChatResourceData(type + "_" + emote.Id, emote.Code, preferredUrl, false, type);
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

		public bool TryGetBttvEmote(string identifier, string userId, out ChatResourceData? customEmote)
		{
			if (_globalBttvData.TryGetValue(identifier, out customEmote) ||
			    _channelBttvData.TryGetValue(userId, out var userSpecificBttvEmotes) && userSpecificBttvEmotes.TryGetValue(identifier, out customEmote))
			{
				return true;
			}

			customEmote = null;
			return false;
		}

		public bool TryGetFfzEmote(string identifier, string userId, out ChatResourceData? customEmote)
		{
			if (_globalFfzData.TryGetValue(identifier, out customEmote) ||
			    _channelFfzData.TryGetValue(userId, out var userSpecificFfzEmotes) && userSpecificFfzEmotes.TryGetValue(identifier, out customEmote))
			{
				return true;
			}

			customEmote = null;
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