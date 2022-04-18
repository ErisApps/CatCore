using System.Threading.Tasks;

namespace CatCore.Helpers
{
	internal static class AsyncEventHandlerDefinitions
	{
		public delegate Task AsyncEventHandler();
		public delegate Task AsyncEventHandler<T1>(T1 t1);
		public delegate Task AsyncEventHandler<T1, T2>(T1 t1, T2 t2);
	}
}