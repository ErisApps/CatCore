using System;
using System.Threading.Tasks;
using CatCore;
using DryIoc;

namespace CatCoreTester
{
	internal static class Program
	{
		private static async Task Main(string[] args)
		{
			var stoppyWatch = new System.Diagnostics.Stopwatch();
			Console.WriteLine("Tester init");

			stoppyWatch.Start();
			var chatCoreInstance = ChatCoreInstance.CreateInstance((level, message) => Console.WriteLine($"External logger: {message}"));
			stoppyWatch.Stop();

			Console.WriteLine($"Tester finished. Instance creation time: {stoppyWatch.Elapsed:g}");
			await Task.Delay(-1).ConfigureAwait(false);
		}
	}
}