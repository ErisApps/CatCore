using BenchmarkDotNet.Running;
using CatCoreBenchmarkSandbox.Benchmarks.TwitchIRCMessageDeconstruction;

namespace CatCoreBenchmarkSandbox
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<TwitchIrcMessageCompoundDeconstructionBenchmark>();
		}
	}
}