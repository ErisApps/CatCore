using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;

namespace CatCoreBenchmarkSandbox.Benchmarks.TwitchPubSub
{
	[MediumRunJob(RuntimeMoniker.Net472, Jit.LegacyJit, Platform.X64)]
	[Orderer(SummaryOrderPolicy.FastestToSlowest)]
	[RankColumn(NumeralSystem.Stars)]
	[MemoryDiagnoser]
	[CategoriesColumn, AllStatisticsColumn, BaselineColumn, MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn, MedianColumn]
	public class TwitchPubSubNonceGenerationBenchmark
	{
		private const string VALID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		private readonly char[] _validCharsArray = VALID_CHARS.ToCharArray();

		[Params(8, 12, 16, 24, 32)]
		public int Length;

		public Random Random;

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
			var sb = new StringBuilder();
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