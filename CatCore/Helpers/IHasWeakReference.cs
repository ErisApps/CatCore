using System;

namespace CatCore.Helpers
{
	public interface IHasWeakReference
	{
		WeakReference WeakReference { get; }
	}
}