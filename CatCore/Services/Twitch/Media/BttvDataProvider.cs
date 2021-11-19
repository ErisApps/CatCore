using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CatCore.Helpers.JSON;
using CatCore.Models.ThirdParty.Bttv.Base;
using CatCore.Models.Twitch.IRC;
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

		// TODO: Change datatype containing emote info like url and such...
		private IReadOnlyDictionary<string, TwitchBadge> _globalBttvData;
		private readonly Dictionary<string, IReadOnlyDictionary<string, TwitchBadge>> _channelBttvData;

		private IReadOnlyDictionary<string, TwitchBadge> _globalFfzData;
		private readonly Dictionary<string, IReadOnlyDictionary<string, TwitchBadge>> _channelFfzData;

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

			_globalBttvData = new Dictionary<string, TwitchBadge>();
			_channelBttvData = new Dictionary<string, IReadOnlyDictionary<string, TwitchBadge>>();

			_globalFfzData = new Dictionary<string, TwitchBadge>();
			_channelFfzData = new Dictionary<string, IReadOnlyDictionary<string, TwitchBadge>>();
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
				var bttvGlobalData = await _bttvApiClient.GetFromJsonAsync(BTTV_API_BASEURL + "emotes/global", BttvSerializerContext.Default.IReadOnlyListBttvEmote).ConfigureAwait(false);
				if (bttvGlobalData == null)
				{
					return false;
				}
				_globalBttvData = ParseEmoteData(bttvGlobalData);

				return true;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the global BTTV emotes");

				return false;
			}
		}

		private async Task<bool> TryRequestGlobalFfzResources()
		{
			try
			{
				var ffzGlobalData = await _bttvApiClient.GetFromJsonAsync(BTTV_API_BASEURL + "frankerfacez/emotes/global", BttvSerializerContext.Default.IReadOnlyListFfzEmote).ConfigureAwait(false);
				if (ffzGlobalData == null)
				{
					return false;
				}

				_globalFfzData = ParseEmoteData(ffzGlobalData);

				return true;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the global FFZ emotes");

				return false;
			}
		}

		private async Task<bool> TryRequestBttvChannelResources(string userId)
		{
			try
			{
				var bttvChannelData = await _bttvApiClient.GetFromJsonAsync(BTTV_API_BASEURL + "users/twitch/" + userId, BttvSerializerContext.Default.BttvChannelData).ConfigureAwait(false);

				_channelBttvData[userId] = ParseEmoteData(bttvChannelData.ChannelEmotes.Concat<EmoteBase>(bttvChannelData.SharedEmotes));

				return true;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the BTTV channel emotes for channel {ChannelId}", userId);

				return false;
			}
		}

		private async Task<bool> TryRequestFfzChannelResources(string userId)
		{
			try
			{
				var ffzChannelData = await _bttvApiClient.GetFromJsonAsync(BTTV_API_BASEURL + "frankerfacez/users/twitch/" + userId, BttvSerializerContext.Default.IReadOnlyListFfzEmote)
					.ConfigureAwait(false);
				if (ffzChannelData == null)
				{
					return false;
				}

				_channelFfzData[userId] = ParseEmoteData(ffzChannelData);

				return true;
			}
			catch (Exception ex)
			{
				_logger.Warning(ex, "Something went wrong while trying to fetch the FFZ channel emotes for channel {ChannelId}", userId);

				return false;
			}
		}

		private ReadOnlyDictionary<string, TwitchBadge> ParseEmoteData(IEnumerable<EmoteBase> emoteData)
		{
			var parsedEmotes = new Dictionary<string, TwitchBadge>();

			// TODO: Implement parsing logic

			return new ReadOnlyDictionary<string, TwitchBadge>(parsedEmotes);
		}


		internal void ReleaseChannelResources(string userId)
		{
			_channelBttvData.Remove(userId);
			_channelFfzData.Remove(userId);
		}

		public bool TryGetEmote(string identifier, string userId, out TwitchBadge? badge)
		{
			if (_channelBttvData.TryGetValue(userId, out var userSpecificBttvEmotes) && userSpecificBttvEmotes.TryGetValue(identifier, out badge) ||
			    _channelFfzData.TryGetValue(userId, out var userSpecificFfzEmotes) && userSpecificFfzEmotes.TryGetValue(identifier, out badge))
			{
				return true;
			}

			return _globalBttvData.TryGetValue(identifier, out badge) || _globalFfzData.TryGetValue(identifier, out badge);
		}
	}
}