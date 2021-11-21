using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CatCore.Models.Twitch.Helix.Responses.Badges;
using CatCore.Models.Twitch.IRC;
using CatCore.Services.Twitch.Interfaces;
using Serilog;

namespace CatCore.Services.Twitch.Media
{
	public class TwitchBadgeDataProvider
	{
		private readonly ILogger _logger;
		private readonly ITwitchHelixApiService _twitchHelixApiService;

		private IReadOnlyDictionary<string, TwitchBadge> _globalBadges;
		private readonly Dictionary<string, IReadOnlyDictionary<string, TwitchBadge>> _channelBadges;

		public TwitchBadgeDataProvider(ILogger logger, ITwitchHelixApiService twitchHelixApiService)
		{
			_logger = logger;
			_twitchHelixApiService = twitchHelixApiService;

			_globalBadges = new ReadOnlyDictionary<string, TwitchBadge>(new Dictionary<string, TwitchBadge>());
			_channelBadges = new Dictionary<string, IReadOnlyDictionary<string, TwitchBadge>>();
		}

		internal async Task TryRequestGlobalResources()
		{
			var globalBadges = await _twitchHelixApiService.GetGlobalBadges().ConfigureAwait(false);
			if (globalBadges == null)
			{
				return;
			}

			_globalBadges = ParseBadgeData("TwitchGlobalBadge_", globalBadges.Value.Data);
		}

		internal async Task TryRequestChannelResources(string userId)
		{
			var channelBadges = await _twitchHelixApiService.GetBadgesForChannel(userId).ConfigureAwait(false);
			if (channelBadges == null)
			{
				return;
			}

			_channelBadges[userId] = ParseBadgeData("TwitchChannelBadge_" + userId, channelBadges.Value.Data);
		}

		internal void ReleaseAllResources()
		{
			_globalBadges = new Dictionary<string, TwitchBadge>();
			_channelBadges.Clear();
		}

		internal void ReleaseChannelResources(string userId)
		{
			_channelBadges.Remove(userId);
		}

		private static ReadOnlyDictionary<string, TwitchBadge> ParseBadgeData(string identifierPrefix, List<BadgeData> badgeData)
		{
			var parsedTwitchBadges = new Dictionary<string, TwitchBadge>();

			foreach (var badge in badgeData)
			{
				foreach (var badgeVersion in badge.Versions)
				{
					var simpleIdentifier = badge.SetId + "/" + badgeVersion.Id;
					parsedTwitchBadges.Add(simpleIdentifier, new TwitchBadge(identifierPrefix + simpleIdentifier, badge.SetId, badgeVersion.ImageUrl4x));
				}
			}

			return new ReadOnlyDictionary<string, TwitchBadge>(parsedTwitchBadges);
		}

		public bool TryGetBadge(out TwitchBadge? badge, string identifier, string userId)
		{
			if (_channelBadges.TryGetValue(userId, out var channelBadges) && channelBadges.TryGetValue(identifier, out badge))
			{
				return true;
			}

			return _globalBadges.TryGetValue(identifier, out badge);
		}
	}
}