using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace CatCoreBenchmarkSandbox.Benchmarks.TwitchIRCMessageDeconstruction
{
	[MemoryDiagnoser]
	[CategoriesColumn, AllStatisticsColumn, BaselineColumn, MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn, MedianColumn]
	public class TwitchIrcMessageTagsDeconstructionBenchmark
	{
		private readonly Regex _chatCoreBaselineRegex = new Regex(@"(?<Tag>[^@^;^=]+)=(?<Value>[^;\s]+)", RegexOptions.Compiled | RegexOptions.Multiline);
		private readonly Regex _suggestedTagsRegex = new Regex(@"([^=]+)=(.*?)(?:$|;)", RegexOptions.Compiled | RegexOptions.Multiline);

		[Params(
			"badge-info=subscriber/1;badges=broadcaster/1,subscriber/0;client-nonce=1ef9899702c12a2081fa33899d7e8465;color=#FF69B4;display-name=RealEris;emotes=;flags=;id=b4595e1c-dd1b-4e45-b7df-a3403c945ad6;mod=0;room-id=405499635;subscriber=1;tmi-sent-ts=1614390981294;turbo=0;user-id=405499635;user-type=",
			"badge-info=founder/13;badges=moderator/1,founder/0,bits/1000;client-nonce=05e5fe0b80aadc4c5035303b99d6762a;color=#DAA520;display-name=Scarapter;emotes=;flags=;id=7317d5aa-38ae-4191-88d7-d4d54a3c27bc;mod=1;room-id=62975335;subscriber=0;tmi-sent-ts=1617644034348;turbo=0;user-id=51591450;user-type=mod",
			"badge-info=;badges=;color=;display-name=bonkeybob;emotes=;flags=;id=108b80a1-7829-4879-86cc-953c3a6b122b;mod=0;room-id=62975335;subscriber=0;tmi-sent-ts=1617644112658;turbo=0;user-id=549616012;user-type=")]
		public string IrcTagsPart = null!;

		[Benchmark(Baseline = true)]
		public Dictionary<string, string> ChatCoreBaselineBenchmark()
		{
			return _chatCoreBaselineRegex.Matches(IrcTagsPart).Cast<Match>().Aggregate(new Dictionary<string, string>(), (dict, m) =>
			{
				dict[m.Groups["Tag"].Value] = m.Groups["Value"].Value;
				return dict;
			});
		}

		[Benchmark]
		public Dictionary<string, string> LinqSplitBenchmark()
		{
			var tags = new Dictionary<string, string>();

			var rawTags = IrcTagsPart.Split(';');
			foreach (var pair in rawTags.Select(tag => tag.Split('=')))
			{
				tags[pair[0]] = pair.Length > 1 ? pair[1] : "true";
			}

			return tags;
		}

		[Benchmark]
		public Dictionary<string, string> RegexDissectionBenchmark()
		{
			var tags = new Dictionary<string, string>();

			foreach (Match match in _suggestedTagsRegex.Matches(IrcTagsPart))
			{
				tags[match.Groups[1].Value] = match.Groups[2].Value;
			}

			return tags;
		}

		[Benchmark]
		public Dictionary<string, string> SpanDissectionBenchmark()
		{
			// Twitch IRC Message spec
			// https://ircv3.net/specs/extensions/message-tags

			var tagsAsSpan = IrcTagsPart.AsSpan();

			var tags = new Dictionary<string, string>();


			// false means looking for separator between key and value, true means looking for separator between
			var lookingForTagSeparator = false;
			var charSeparator = '=';
			var startPos = 0;

			string? keyTmp = null;

			for (var curPos = 0; curPos < tagsAsSpan.Length; curPos++)
			{
				if (tagsAsSpan[curPos] == charSeparator)
				{
					if (lookingForTagSeparator)
					{
						tags[keyTmp!] = (curPos == startPos) ? string.Empty : tagsAsSpan.Slice(startPos, curPos - startPos).ToString();

						lookingForTagSeparator = false;
						charSeparator = '=';
						startPos = curPos + 1;
					}
					else
					{
						keyTmp = tagsAsSpan.Slice(startPos, curPos - startPos).ToString();

						lookingForTagSeparator = true;
						charSeparator = ';';
						startPos = curPos + 1;
					}
				}
			}

			return tags;
		}

		[Benchmark]
		public Dictionary<string, string> SpanDissectionBenchmarkV2()
		{
			// Twitch IRC Message spec
			// https://ircv3.net/specs/extensions/message-tags

			var tagsAsSpan = IrcTagsPart.AsSpan();

			var tags = new Dictionary<string, string>();


			// false means looking for separator between key and value, true means looking for separator between
			var charSeparator = '=';
			var startPos = 0;

			string? keyTmp = null;

			for (var curPos = 0; curPos < tagsAsSpan.Length; curPos++)
			{
				if (tagsAsSpan[curPos] == charSeparator)
				{
					if (charSeparator == ';')
					{
						tags[keyTmp!] = (curPos == startPos) ? string.Empty : tagsAsSpan.Slice(startPos, curPos - startPos).ToString();

						charSeparator = '=';
						startPos = curPos + 1;
					}
					else
					{
						keyTmp = tagsAsSpan.Slice(startPos, curPos - startPos).ToString();

						charSeparator = ';';
						startPos = curPos + 1;
					}
				}
			}

			return tags;
		}

		[Benchmark]
		// ReSharper disable once CognitiveComplexity
		public Dictionary<string, string> SpanDissectionBenchmarkV3()
		{
			// Twitch IRC Message spec
			// https://ircv3.net/specs/extensions/message-tags

			var tagsAsSpan = IrcTagsPart.AsSpan();

			var tags = new Dictionary<string, string>();


			// false means looking for separator between key and value, true means looking for separator between
			var charSeparator = '=';
			var startPos = 0;
			int curPos;

			ReadOnlySpan<char> keyTmp = null;
			for (curPos = 0; curPos < tagsAsSpan.Length; curPos++)
			{
				if (tagsAsSpan[curPos] == charSeparator)
				{
					if (charSeparator == ';')
					{
						if (curPos != startPos)
						{
							tags[keyTmp.ToString()] = tagsAsSpan.Slice(startPos, curPos - startPos).ToString();
						}

						charSeparator = '=';
						startPos = curPos + 1;
					}
					else
					{
						keyTmp = tagsAsSpan.Slice(startPos, curPos - startPos);

						charSeparator = ';';
						startPos = curPos + 1;
					}
				}
			}

			if (curPos != startPos)
			{
				tags[keyTmp.ToString()] = tagsAsSpan.Slice(startPos, curPos - startPos).ToString();
			}

			return tags;
		}

		// Kept to 2 benchmarks below to measure the performance impact of the fix
		[Benchmark]
		public Dictionary<string, string> SpanDissectionBenchmarkBroken()
		{
			// Twitch IRC Message spec
			// https://ircv3.net/specs/extensions/message-tags

			var tagsAsSpan = IrcTagsPart.AsSpan();

			var tags = new Dictionary<string, string>();


			// false means looking for separator between key and value, true means looking for separator between
			var lookingForTagSeparator = false;
			var charSeparator = '=';
			var startPos = 0;

			string? keyTmp = null;

			for (var curPos = 0; curPos < tagsAsSpan.Length; curPos++)
			{
				if (tagsAsSpan[curPos] == charSeparator)
				{
					if (lookingForTagSeparator)
					{
						tags[keyTmp!] = (curPos == startPos) ? string.Empty : tagsAsSpan.Slice(startPos, curPos - startPos - 1).ToString();

						lookingForTagSeparator = false;
						charSeparator = '=';
						startPos = curPos + 1;
					}
					else
					{
						keyTmp = tagsAsSpan.Slice(startPos, curPos - startPos - 1).ToString();

						lookingForTagSeparator = true;
						charSeparator = ';';
						startPos = curPos + 1;
					}
				}
			}

			return tags;
		}


		[Benchmark]
		public Dictionary<string, string> SpanDissectionBenchmarkV2Broken()
		{
			// Twitch IRC Message spec
			// https://ircv3.net/specs/extensions/message-tags

			var tagsAsSpan = IrcTagsPart.AsSpan();

			var tags = new Dictionary<string, string>();


			// false means looking for separator between key and value, true means looking for separator between
			var charSeparator = '=';
			var startPos = 0;

			string? keyTmp = null;

			for (var curPos = 0; curPos < tagsAsSpan.Length; curPos++)
			{
				if (tagsAsSpan[curPos] == charSeparator)
				{
					if (charSeparator == ';')
					{
						tags[keyTmp!] = (curPos == startPos) ? string.Empty : tagsAsSpan.Slice(startPos, curPos - startPos - 1).ToString();

						charSeparator = '=';
						startPos = curPos + 1;
					}
					else
					{
						keyTmp = tagsAsSpan.Slice(startPos, curPos - startPos - 1).ToString();

						charSeparator = ';';
						startPos = curPos + 1;
					}
				}
			}

			return tags;
		}
	}
}