using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace CatCoreBenchmarkSandbox.Benchmarks.Miscellaneous
{
	[MemoryDiagnoser]
	[CategoriesColumn, AllStatisticsColumn, BaselineColumn, MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn, MedianColumn]
	public class RandomColorGenerationBenchmark
	{
		private const string VALID_CHARS = "0123456789ABCDEF";
		private readonly char[] _validCharsArray = VALID_CHARS.ToCharArray();

		[Params(6)]
		public int Length;

		public Random Random = null!;

		[GlobalSetup]
		public void GlobalSetup()
		{
			Random = new Random(8);
		}

		[Benchmark(Baseline = true)]
		public string ChatCoreBaselineBenchmark()
		{
			var argb = (Random.Next(255) << 16) + (Random.Next(255) << 8) + Random.Next(255);
			return $"#{argb:X6}FF";
		}

		[Benchmark]
		public string StringLinqBenchmark()
		{
			return new string(Enumerable.Repeat(VALID_CHARS, Length).Select(s => s[Random.Next(s.Length)]).ToArray());
		}

		[Benchmark]
		public string StringLoopBenchmark()
		{
			var stringChars = new char[Length];
			for (var i = 0; i < stringChars.Length; i++)
			{
				stringChars[i] = VALID_CHARS[Random.Next(VALID_CHARS.Length)];
			}

			return new string(stringChars);
		}

		[Benchmark]
		public string StringCharArrayLoopBenchmark()
		{
			var stringChars = new char[Length];
			for (var i = 0; i < stringChars.Length; i++)
			{
				stringChars[i] = _validCharsArray[Random.Next(_validCharsArray.Length)];
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
				sb.Append(charsAsSpan[Random.Next(charsAsSpan.Length)]);
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
				charsAsSpan.Slice(Random.Next(charsAsSpan.Length), 1).CopyTo(resultSpan.Slice(i));
			}

			return new string(result);
		}
	}
}