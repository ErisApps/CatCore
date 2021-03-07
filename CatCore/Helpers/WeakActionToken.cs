using System;

namespace CatCore.Helpers
{
	internal static class WeakActionToken
	{
		public static IDisposable Create<TTarget>(TTarget target, Action<TTarget>? action) where TTarget : class
		{
			return new WeakActionTokenInternal<TTarget>(target, action);
		}

		public static WeakReference WeakReferenceForItem(object item)
		{
			return item is IHasWeakReference hasWeak ? hasWeak.WeakReference : CreateWeakReference(item);
		}

		private static WeakReference GetWeakReference(object target)
		{
			return target as WeakReference ?? WeakReferenceForItem(target);
		}

		private static WeakReference CreateWeakReference(object o)
		{
			return new WeakReference(o, false);
		}

		private sealed class WeakActionTokenInternal<TTarget> : IDisposable where TTarget : class
		{
			private object _target;
			private object? _action;

			public WeakActionTokenInternal(TTarget target, Action<TTarget>? action = null)
			{
				_target = GetWeakReference(target);
				_action = action;
			}

			public void Dispose()
			{
				if (_action == null)
				{
					return;
				}

				object action;
				lock (_target)
				{
					if (_action == null)
					{
						return;
					}

					action = _action;
					_action = null!;
				}

				if (_target is WeakReference weakReference)
				{
					var target = (TTarget)weakReference.Target;
					((Action<TTarget>)action).Invoke(target);
				}

				_target = null!;
			}
		}
	}
}