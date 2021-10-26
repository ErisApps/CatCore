using System;

namespace CatCore.Exceptions
{
	public class CatCoreNotInitializedException : Exception
	{
		public override string Message => $"{nameof(CatCore)} not initialized. Make sure to call {nameof(CatCoreInstance)}.{nameof(CatCoreInstance.Create)}() to initialize ChatCore!";
	}
}