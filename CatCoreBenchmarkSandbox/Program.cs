using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using CatCoreBenchmarkSandbox.Benchmarks.TwitchIRCMessageDeconstruction;

namespace CatCoreBenchmarkSandbox
{
	internal class Program
	{
		private const string MONO_PATH_UNITY_CURRENT = @"D:\Program Files\Unity\2019.4.18f1\Editor\Data\MonoBleedingEdge\bin\mono.exe";
		private const string MONO_PATH_UNITY_BETA = @"D:\Program Files\Unity\2021.2.0b4\Editor\Data\MonoBleedingEdge\bin\mono.exe";

		public static void Main()
		{
			var columns = new List<IColumn>();
			columns.Add(CategoriesColumn.Default);
			columns.Add(TargetMethodColumn.Method);
			columns.AddRange(JobCharacteristicColumn.AllColumns);
			columns.AddRange(StatisticColumn.AllStatistics);
			columns.Add(BaselineRatioColumn.RatioMean);
			columns.Add(RankColumn.Stars);
			columns.Add(BaselineColumn.Default);

			var benchmarkConfiguration = ManualConfig.CreateEmpty()
					.AddJob(Job.Default
						.WithRuntime(ClrRuntime.Net472)
						.AsBaseline())
					.AddJob(Job.Default
						.WithRuntime(CoreRuntime.Core50))
					.AddJob(Job.Default
						.WithRuntime(new MonoRuntime("Mono Unity 2019.4.18f1", MONO_PATH_UNITY_CURRENT)))
					.AddJob(Job.Default
						.WithRuntime(new MonoRuntime("Mono Unity 2021.2.0b4", MONO_PATH_UNITY_BETA)))
					.WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest))
					.AddDiagnoser(MemoryDiagnoser.Default)
					.AddColumnProvider(DefaultColumnProviders.Instance)
					.AddLogger(ConsoleLogger.Default)
					.AddExporter(BenchmarkReportExporter.Default, HtmlExporter.Default, MarkdownExporter.Console);

			BenchmarkRunner.Run<TwitchIrcMessageCompoundDeconstructionBenchmark>(benchmarkConfiguration);
		}
	}
}