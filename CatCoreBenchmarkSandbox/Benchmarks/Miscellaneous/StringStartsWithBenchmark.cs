using System;
using BenchmarkDotNet.Attributes;

namespace CatCoreBenchmarkSandbox.Benchmarks.Miscellaneous
{
	[MemoryDiagnoser]
	[CategoriesColumn, AllStatisticsColumn, BaselineColumn, MinColumn, Q1Column, MeanColumn, Q3Column, MaxColumn, MedianColumn]
	public class StringStartsWithBenchmark
	{
		[Params(
			"ACTION Heya",
			"ACTION i definitely dont miss the mass of random charging ports that existed",
			"ACTION The phrase âitâs just a gameâ is such a weak mindset. You are ok with what happened, losing, imperfection of a craft. When you stop getting angry after losing, youâve lost twice.   Thereâs always something to learn, and always room for improvement, never settle.",
			"ACTION Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop p")]
		public string Message = null!;

		[Benchmark(Baseline = true)]
		public bool StringStartsWith()
		{
			const string ACTION = "ACTION ";
			return Message.StartsWith(ACTION, StringComparison.Ordinal);
		}

		[Benchmark]
		public bool SpanStartsWith()
		{
			const string ACTION = "ACTION ";
			return Message.AsSpan().StartsWith(ACTION.AsSpan(), StringComparison.Ordinal);
		}
	}
}