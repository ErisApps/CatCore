using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CatCore.Models.Twitch.Helix.Responses.Bits.Cheermotes;
using CatCore.Models.Twitch.Media;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.Media
{
	public class TwitchCheermoteDataProvider
	{
		private readonly ILogger _logger;
		private readonly ITwitchHelixApiService _twitchHelixApiService;

		private IReadOnlyDictionary<string, IReadOnlyList<TwitchCheermote>> _globalCheermotes;
		private readonly Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<TwitchCheermote>>> _channelCheermotes;

		public TwitchCheermoteDataProvider(ILogger logger, ITwitchHelixApiService twitchHelixApiService)
		{
			_logger = logger;
			_twitchHelixApiService = twitchHelixApiService;

			_globalCheermotes = new ReadOnlyDictionary<string, IReadOnlyList<TwitchCheermote>>(new Dictionary<string, IReadOnlyList<TwitchCheermote>>());
			_channelCheermotes = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<TwitchCheermote>>>();
		}

		internal async Task<bool> TryRequestGlobalResources()
		{
			var globalCheermotes = await _twitchHelixApiService.GetCheermotes().ConfigureAwait(false);
			if (globalCheermotes == null)
			{
				return false;
			}

			_globalCheermotes = ParseCheermoteData(globalCheermotes.Value.Data);

			return true;
		}

		internal async Task<bool> TryRequestChannelResources(string userId)
		{
			var channelCheermotes = await _twitchHelixApiService.GetCheermotes(userId).ConfigureAwait(false);
			if (channelCheermotes == null)
			{
				return false;
			}

			_channelCheermotes[userId] = ParseCheermoteData(channelCheermotes.Value.Data.Where(x => x.Type == CheermoteType.ChannelCustom));

			return true;
		}

		private ReadOnlyDictionary<string, IReadOnlyList<TwitchCheermote>> ParseCheermoteData(IEnumerable<CheermoteGroupData> cheermoteGroupData)
		{
			var parsedCheermotes = new Dictionary<string, IReadOnlyList<TwitchCheermote>>();

			foreach (var cheermoteData in cheermoteGroupData)
			{
				var cheermoteTiers = new List<TwitchCheermote>();
				foreach (var cheermoteTier in cheermoteData.Tiers.OrderBy(x => x.MinBits))
				{
					var url = cheermoteTier.Images.Dark.Animated.Size4;
					cheermoteTiers.Add(new TwitchCheermote(cheermoteData.Prefix + cheermoteTier.MinBits, url, true, cheermoteTier.MinBits, cheermoteTier.Color, cheermoteTier.CanCheer));
				}

				parsedCheermotes[cheermoteData.Prefix] = cheermoteTiers;
			}

			return new ReadOnlyDictionary<string, IReadOnlyList<TwitchCheermote>>(parsedCheermotes);
		}

		internal void ReleaseAllResources()
		{
			_globalCheermotes = new Dictionary<string, IReadOnlyList<TwitchCheermote>>();
			_channelCheermotes.Clear();
		}

		internal void ReleaseChannelResources(string userId)
		{
			_channelCheermotes.Remove(userId);
		}

		public bool TryGetCheermote(string identifier, string userId, uint minBits, out TwitchCheermote? cheermote)
		{
			if (_channelCheermotes.TryGetValue(userId, out var channelBadges) && channelBadges.TryGetValue(identifier, out var cheermoteTiers))
			{
				cheermote = GetCheermoteTier(cheermoteTiers, minBits);
				return true;
			}

			if (_globalCheermotes.TryGetValue(identifier, out cheermoteTiers))
			{
				cheermote = GetCheermoteTier(cheermoteTiers, minBits);
				return true;
			}

			cheermote = null;
			return false;
		}

		private static TwitchCheermote GetCheermoteTier(IReadOnlyList<TwitchCheermote> tiers, uint numBits)
		{
			for (var i = 1; i < tiers.Count; i++)
			{
				if (numBits < tiers[i].MinBits)
				{
					return tiers[i - 1];
				}
			}

			return tiers.Last();
		}
	}
}