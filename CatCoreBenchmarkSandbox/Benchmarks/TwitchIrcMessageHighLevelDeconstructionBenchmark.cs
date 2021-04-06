using System;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;

namespace CatCoreBenchmarkSandbox.Benchmarks
{
	[MediumRunJob(RuntimeMoniker.Net472, Jit.LegacyJit, Platform.X64)]
	[Orderer(SummaryOrderPolicy.FastestToSlowest)]
	[RankColumn(NumeralSystem.Stars)]
	[CategoriesColumn, AllStatisticsColumn, BaselineColumn, MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn, MedianColumn]
	public class TwitchIrcMessageHighLevelDeconstructionBenchmark
	{
		private readonly Regex _twitchMessageRegex =
			new Regex(
				@"^(?:@(?<Tags>[^\r\n ]*) +|())(?::(?<HostName>[^\r\n ]+) +|())(?<MessageType>[^\r\n ]+)(?: +(?<ChannelName>[^:\r\n ]+[^\r\n ]*(?: +[^:\r\n ]+[^\r\n ]*)*)|())?(?: +:(?<Message>[^\r\n]*)| +())?[\r\n]*$",
				RegexOptions.Compiled | RegexOptions.Multiline);

		[Params(
			":tmi.twitch.tv 376 realeris :>",
			":realeris!realeris@realeris.tmi.twitch.tv JOIN #realeris",
			":realeris.tmi.twitch.tv 353 realeris = #realeris :realeris",
			":realeris.tmi.twitch.tv 366 realeris #realeris :End of /NAMES list",
			":tmi.twitch.tv CAP * ACK :twitch.tv/tags twitch.tv/commands twitch.tv/membership",
			"@badge-info=subscriber/1;badges=broadcaster/1,subscriber/0;client-nonce=1ef9899702c12a2081fa33899d7e8465;color=#FF69B4;display-name=RealEris;emotes=;flags=;id=b4595e1c-dd1b-4e45-b7df-a3403c945ad6;mod=0;room-id=405499635;subscriber=1;tmi-sent-ts=1614390981294;turbo=0;user-id=405499635;user-type= :realeris!realeris@realeris.tmi.twitch.tv PRIVMSG #realeris :Heya")]
		public string IrcMessage;

		[Benchmark(Baseline = true)]
		public void RegexBenchmark()
		{
			var match = _twitchMessageRegex.Match(IrcMessage);
		}

		[Benchmark]
		public void SpanDissectionBenchmark()
		{
			// Twitch IRC Message spec
			// https://ircv3.net/specs/extensions/message-tags

			string tags = null;
			string hostname = null;
			string messageType = null;
			string channelName = null;
			string message = null;

			var position = 0;
			var nextSpacePosition = 0;

			var messageAsSpan = IrcMessage.AsSpan();

			void SkipToNextNonSpaceCharacter(in ReadOnlySpan<char> msg)
			{
				while (position < msg.Length && msg[position] == ' ')
				{
					position++;
				}
			}

			// Check for message tags
			if (messageAsSpan[0] == '@')
			{
				nextSpacePosition = messageAsSpan.IndexOf(' ');
				if (nextSpacePosition == -1)
				{
					throw new Exception("Invalid IRC Message");
				}

				tags = messageAsSpan.Slice(1, nextSpacePosition - 1).ToString();

				position = nextSpacePosition + 1;
				SkipToNextNonSpaceCharacter(messageAsSpan);
				messageAsSpan = messageAsSpan.Slice(position);
				position = 0;
			}


			// Handle hostname
			if (messageAsSpan[position] == ':')
			{
				nextSpacePosition = messageAsSpan.IndexOf(' ');
				if (nextSpacePosition == -1)
				{
					throw new Exception("Invalid IRC Message");
				}

				hostname = messageAsSpan.Slice(1, (nextSpacePosition) - 1).ToString();

				position = nextSpacePosition + 1;
				SkipToNextNonSpaceCharacter(messageAsSpan);
				messageAsSpan = messageAsSpan.Slice(position);
				position = 0;
			}


			// Handle MessageType
			nextSpacePosition = messageAsSpan.IndexOf(' ');
			if (nextSpacePosition == -1)
			{
				if (messageAsSpan.Length > position)
				{
					messageType = messageAsSpan.ToString();
				}
			}

			messageType = messageAsSpan.Slice(0, nextSpacePosition).ToString();
			position = nextSpacePosition + 1;
			SkipToNextNonSpaceCharacter(messageAsSpan);
			messageAsSpan = messageAsSpan.Slice(position);
			position = 0;

			// Handle ChannelName
			nextSpacePosition = messageAsSpan.IndexOf(' ');
			if (nextSpacePosition == -1)
			{
				if (messageAsSpan.Length > position)
				{
					channelName = messageAsSpan.ToString();
				}
			}

			channelName = messageAsSpan.Slice(0, nextSpacePosition).ToString();
			position = nextSpacePosition + 1;
			SkipToNextNonSpaceCharacter(messageAsSpan);
			messageAsSpan = messageAsSpan.Slice(position);
			position = 0;

			// Handle Message
			if (messageAsSpan[position] == ':')
			{
				message = messageAsSpan.Slice(1).ToString();
			}
		}
	}
}