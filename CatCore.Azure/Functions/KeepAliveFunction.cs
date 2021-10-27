using Microsoft.Azure.Functions.Worker;

namespace CatCore.Azure.Functions
{
	public static class KeepAliveFunction
	{
		[Function("KeepAliveFunction")]
		public static void Run([TimerTrigger("0 */10 * * * *")] TimerInfo timerInfo, FunctionContext context)
		{
		}
	}
}