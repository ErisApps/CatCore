using System;
using System.Threading.Tasks;

namespace CatCoreTester
{
	internal static class Program
	{
		private static async Task Main(string[] args)
		{
			await Task.Delay(-1).ConfigureAwait(false);
		}
	}
}