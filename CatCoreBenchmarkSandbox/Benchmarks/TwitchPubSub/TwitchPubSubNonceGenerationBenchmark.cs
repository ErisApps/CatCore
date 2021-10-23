using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace CatCoreBenchmarkSandbox.Benchmarks.TwitchPubSub
{
	[MemoryDiagnoser]
	[CategoriesColumn, AllStatisticsColumn, BaselineColumn, MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn, MedianColumn]
	public class TwitchPubSubNonceGenerationBenchmark
	{
		private const string VALID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		private const int VALID_CHARS_LENGTH = 36;

		private readonly char[] _validCharsArray = VALID_CHARS.ToCharArray();

		[Params(8, 12, 16, 24, 32)]
		public int Length;

		public Random Random = null!;

		[GlobalSetup]
		public void GlobalSetup()
		{
			Random = new Random(8);
		}

		[Benchmark(Baseline = true)]
		public string GuidBenchmark()
		{
			return Guid.NewGuid().ToString("N").Substring(0, Length);
		}

		[Benchmark]
		public string StringLinqBenchmark()
		{
			return new string(Enumerable.Repeat(VALID_CHARS, Length).Select(s => s[Random.Next(VALID_CHARS_LENGTH)]).ToArray());
		}

		[Benchmark]
		public string StringLoopBenchmark()
		{
			var stringChars = new char[Length];
			for (var i = 0; i < stringChars.Length; i++)
			{
				stringChars[i] = VALID_CHARS[Random.Next(VALID_CHARS_LENGTH)];
			}

			return new string(stringChars);
		}

		[Benchmark]
		public string StringCharArrayLoopBenchmark()
		{
			var stringChars = new char[Length];
			for (var i = 0; i < stringChars.Length; i++)
			{
				stringChars[i] = _validCharsArray[Random.Next(VALID_CHARS_LENGTH)];
			}

			return new string(stringChars);
		}

		[Benchmark]
		public string StringBuilderSpanBenchmark()
		{
			var charsAsSpan = VALID_CHARS.AsSpan();
			var sb = new StringBuilder(Length);
			for (var i = 0; i < Length; i++)
			{
				sb.Append(charsAsSpan[Random.Next(VALID_CHARS_LENGTH)]);
			}

			return sb.ToString();
		}

		[Benchmark]
		public string SpanBufferBenchmark()
		{
			var charsAsSpan = VALID_CHARS.AsSpan();

			var result = new char[Length];
			var resultSpan = result.AsSpan();
			for (var i = 0; i < result.Length; i++)
			{
				charsAsSpan.Slice(Random.Next(VALID_CHARS_LENGTH), 1).CopyTo(resultSpan.Slice(i));
			}

			return new string(result);
		}
	}
}