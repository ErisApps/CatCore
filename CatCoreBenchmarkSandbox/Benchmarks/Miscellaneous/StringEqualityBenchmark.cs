using BenchmarkDotNet.Attributes;

namespace CatCoreBenchmarkSandbox.Benchmarks.Miscellaneous
{
	internal static class IrcCommands
	{
		public const string RPL_ENDOFMOTD = "376";
		public const string PING = nameof(PING);
		public const string PONG = nameof(PONG);
		public const string JOIN = nameof(JOIN);
		public const string PART = nameof(PART);
		public const string NOTICE = nameof(NOTICE);
		public const string PRIVMSG = nameof(PRIVMSG);
	}

	internal static class TwitchIrcCommands
	{
		public const string CLEARCHAT = nameof(CLEARCHAT);
		public const string CLEARMSG = nameof(CLEARMSG);
		public const string GLOBALUSERSTATE = nameof(GLOBALUSERSTATE);
		public const string ROOMSTATE = nameof(ROOMSTATE);
		public const string USERNOTICE = nameof(USERNOTICE);
		public const string USERSTATE = nameof(USERSTATE);
		public const string RECONNECT = nameof(RECONNECT);
		public const string HOSTTARGET = nameof(HOSTTARGET);
	}

	[MemoryDiagnoser]
	[CategoriesColumn, AllStatisticsColumn, BaselineColumn, MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn, MedianColumn]
	public class StringEqualityBenchmark
	{
		[Params(IrcCommands.PING, IrcCommands.NOTICE, IrcCommands.PRIVMSG, TwitchIrcCommands.GLOBALUSERSTATE, TwitchIrcCommands.USERNOTICE)]
		public string CommandType = null!;

		[Benchmark(Baseline = true)]
		public bool StringEqualityWithoutPattern() {
			// ReSharper disable once MergeIntoLogicalPattern
			return CommandType == IrcCommands.NOTICE || CommandType == TwitchIrcCommands.USERNOTICE;
		}

		[Benchmark]
		public bool StringEqualityWithPattern()
		{
			return CommandType is IrcCommands.NOTICE or TwitchIrcCommands.USERNOTICE;
		}
	}
}