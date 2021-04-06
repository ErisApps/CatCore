using System;

namespace CatCore.Exceptions
{
	public class CatCoreNotInitializedException : Exception
	{
		public override string Message => $"{nameof(CatCore)} not initialized. Make sure to call {nameof(ChatCoreInstance)}.{nameof(ChatCoreInstance.CreateInstance)}() to initialize ChatCore!";
	}
}