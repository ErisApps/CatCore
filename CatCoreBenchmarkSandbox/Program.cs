using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace CatCoreBenchmarkSandbox
{
	internal class Program
	{
		private static readonly List<MonoRuntime> MonoRuntimes = new List<MonoRuntime>
		{
			new MonoRuntime("Mono Unity 2019.4.18f1", @"D:\Program Files\Unity\2019.4.18f1\Editor\Data\MonoBleedingEdge\bin\mono.exe"),
			new MonoRuntime("Mono Unity 2021.2.0b4", @"D:\Program Files\Unity\2021.2.0b4\Editor\Data\MonoBleedingEdge\bin\mono.exe")
		};

		public static void Main()
		{
			var benchmarkConfiguration = ManualConfig.CreateEmpty()
				.AddJob(Job.Default
					.WithRuntime(ClrRuntime.Net472)
					.AsBaseline())
				.AddJob(Job.Default
					.WithRuntime(CoreRuntime.Core50))
				.AddJob(MonoRuntimes
					.Select(runtimeEntry => Job.Default.WithRuntime(runtimeEntry))
					.ToArray())
				.WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest))
				.AddDiagnoser(MemoryDiagnoser.Default)
				.AddColumnProvider(DefaultColumnProviders.Instance)
				.AddLogger(ConsoleLogger.Default)
				.AddExporter(BenchmarkReportExporter.Default, HtmlExporter.Default, MarkdownExporter.Console);

			BenchmarkRunner.Run<Benchmarks.TwitchPubSub.TwitchPubSubNonceGenerationBenchmark>(benchmarkConfiguration);
		}
	}
}