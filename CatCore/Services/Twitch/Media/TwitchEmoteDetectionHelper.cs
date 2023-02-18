using System.Collections.Generic;
using System.Linq;
using System.Text;
using CatCore.Models.Shared;
using CatCore.Models.Twitch.IRC;
using CatCore.Models.Twitch.Media;
using CatCore.Services.Interfaces;

namespace CatCore.Services.Twitch.Media
{
	internal class TwitchEmoteDetectionHelper
	{
		private readonly IKittenSettingsService _settingsService;
		private readonly TwitchMediaDataProvider _twitchMediaDataProvider;

		public TwitchEmoteDetectionHelper(
			IKittenSettingsService settingsService,
			TwitchMediaDataProvider twitchMediaDataProvider)
		{
			_settingsService = settingsService;
			_twitchMediaDataProvider = twitchMediaDataProvider;
		}

		public List<IChatEmote> ExtractEmoteInfo(string message, IReadOnlyDictionary<string, string>? messageMeta, string channelId, uint bits)
		{
			var emotes = new List<IChatEmote>();

			var twitchConfig = _settingsService.Config.TwitchConfig;
			if (twitchConfig.ParseTwitchEmotes && messageMeta != null)
			{
				ExtractTwitchEmotes(emotes, message, messageMeta);
			}

			if (_settingsService.Config.GlobalConfig.HandleEmojis)
			{
				ExtractEmojis(emotes, message);
			}

			ExtractOtherEmotes(emotes, message, channelId, twitchConfig.ParseCheermotes && bits > 0, twitchConfig.ParseBttvEmotes || twitchConfig.ParseFfzEmotes);

			return emotes;
		}

		private static void ExtractTwitchEmotes(List<IChatEmote> emotes, string message, IReadOnlyDictionary<string, string> messageMeta)
		{
			if (!messageMeta.TryGetValue(IrcMessageTags.EMOTES, out var emotesString))
			{
				return;
			}

			var twitchEmoteOffsetCorrectorHelper = new TwitchEmoteOffsetCorrectorHelper(message);

			var emoteGroup = emotesString.Split('/');
			for (var i = 0; i < emoteGroup.Length; i++)
			{
				var emoteSet = emoteGroup[i].Split(':');
				var emoteId = emoteSet[0];

				var prefixedEmoteId = "TwitchEmote_" + emoteId;

				var emotePlaceholders = emoteSet[1].Split(',');

				for (var j = 0; j < emotePlaceholders.Length; j++)
				{
					var emoteMeta = emotePlaceholders[j].Split('-');

					var emoteStart = int.Parse(emoteMeta[0]);
					var emoteEnd = int.Parse(emoteMeta[1]);
					var emoteLength = emoteEnd - emoteStart + 1;

					var offset = twitchEmoteOffsetCorrectorHelper.CalculateOffset(emoteStart);

					emoteStart += offset;
					emoteEnd += offset;

					var messageSubstring = message.Substring(emoteStart, emoteLength);

					var emoteUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{emoteId}/static/dark/3.0";

					emotes.Add(new TwitchEmote(prefixedEmoteId, messageSubstring, emoteStart, emoteEnd, emoteUrl));
				}
			}
		}

		private static void ExtractEmojis(List<IChatEmote> emotes, string message)
		{
			for (var i = 0; i < message.Length; i++)
			{
				var foundEmojiLeaf = Emoji.Twemoji.Emojis.EmojiReferenceData.LookupLeaf(message, i);
				if (foundEmojiLeaf != null)
				{
					emotes.Add(new Models.Shared.Emoji(foundEmojiLeaf.Key, foundEmojiLeaf.Key, i, i += foundEmojiLeaf.Depth, foundEmojiLeaf.Url));
				}
			}
		}

		// ReSharper disable once CognitiveComplexity
		private void ExtractOtherEmotes(List<IChatEmote> emotes, string message, string channelId, bool parseCheermotes, bool parseCustomEmotes)
		{
			if (!parseCheermotes && !parseCustomEmotes)
			{
				return;
			}

			void ExtractOtherEmotesInternal(int messageStartIndex, int messageEndIndex)
			{
				var currentWordBuilder = new StringBuilder();
				for (var i = messageStartIndex; i <= messageEndIndex; i++)
				{
					if (i == messageEndIndex || char.IsWhiteSpace(message[i]))
					{
						if (currentWordBuilder.Length <= 0)
						{
							continue;
						}

						var currentWord = currentWordBuilder.ToString();

						if (parseCustomEmotes && _twitchMediaDataProvider.TryGetThirdPartyEmote(currentWord, channelId, out var customEmote))
						{
							var startIndex = i - currentWord.Length;
							var endIndex = i - 1;

							emotes.Add(new TwitchEmote(customEmote!.Id, customEmote.Name, startIndex, endIndex, customEmote.Url, customEmote.IsAnimated));
						}
						else if (parseCheermotes && _twitchMediaDataProvider.TryGetCheermote(currentWord, channelId, out var emoteBits, out var cheermoteData))
						{
							var startIndex = i - currentWord.Length;
							var endIndex = i - 1;

							emotes.Add(new TwitchEmote(cheermoteData!.Id, cheermoteData.Name, startIndex, endIndex, cheermoteData.Url, cheermoteData.IsAnimated, emoteBits, cheermoteData.Color));
						}

						currentWordBuilder.Clear();
					}
					else
					{
						currentWordBuilder.Append(message[i]);
					}
				}
			}

			var orderedEmotesList = emotes.OrderBy(x => x.StartIndex).ToList();
			var loopStartIndex = 0;
			foreach (var referenceEmote in orderedEmotesList)
			{
				ExtractOtherEmotesInternal(loopStartIndex, referenceEmote.StartIndex - 1);

				loopStartIndex = referenceEmote.EndIndex + 2;
			}

			ExtractOtherEmotesInternal(loopStartIndex, message.Length);
		}

		/// <summary>
		/// This helper is needed to calculate the correct offset for twitch emote indices when there are preceding surrogate pairs in the message,
		/// as the Twitch treats a surrogate pair as a single character.
		/// </summary>
		private class TwitchEmoteOffsetCorrectorHelper
		{
			private readonly int[] _surrogatePairDescriptors;

			private readonly bool _shouldNotCalculateOffset;

			public TwitchEmoteOffsetCorrectorHelper(string message)
			{
				_surrogatePairDescriptors = CalculateSurrogatePairInfo(message).ToArray();

				_shouldNotCalculateOffset = _surrogatePairDescriptors.Length == 0;
			}

			public int CalculateOffset(int startIndex)
			{
				if (_shouldNotCalculateOffset)
				{
					return 0;
				}

				var offset = 0;
				for (var i = 0; i < _surrogatePairDescriptors.Length; i++)
				{
					var surrogatePairStartIndex = _surrogatePairDescriptors[i];
					if (startIndex < surrogatePairStartIndex)
					{
						break;
					}

					offset++;
					startIndex++;
				}

				return offset;
			}

			private static IEnumerable<int> CalculateSurrogatePairInfo(string message)
			{
				for (var i = 0; i < message.Length; i += 2)
				{
					if (char.IsSurrogate(message[i]))
					{
						yield return i;
					}
				}
			}
		}
	}
}