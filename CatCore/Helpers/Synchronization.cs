using System;
using System.Threading;
using System.Threading.Tasks;

namespace CatCore.Helpers
{
	/// <summary>
	/// Utilities for inter-thread synchronization. All Locker method acquire their object immediately,
	/// and should only be used with <see langword="using"/> to automatically release them.
	/// </summary>
	/// <example>
	/// <para>
	/// The canonical usage of *all* of the member functions is as follows, substituting <see cref="Lock(SemaphoreSlim)"/>
	/// with whichever member you want to use, according to your lock type.
	/// </para>
	/// <code>
	/// using var _locker = Synchronization.Lock(semaphoreSlim);
	/// </code>
	/// </example>
	/// <remarks>
	/// This code is roughly based upon the Synchronization class in BSIPA.
	/// https://github.com/bsmg/BeatSaber-IPA-Reloaded/blob/fd5b082feceef5d9ee6878b13a9db1865afad61f/IPA.Loader/Utilities/Async/Synchronization.cs
	/// </remarks>
	internal static class Synchronization
	{
		/// <summary>
		/// Creates a locker for a semaphore.
		/// </summary>
		/// <param name="s">the semaphore to acquire</param>
		/// <returns>the locker to use with <see langword="using"/></returns>
		public static IDisposable Lock(Semaphore s)
		{
			s.WaitOne();
			return WeakActionToken.Create(s, locker => locker.Release());
		}

		/// <summary>
		/// Creates a locker for a slim semaphore.
		/// </summary>
		/// <param name="ss">the slim semaphore to acquire</param>
		/// <returns>the locker to use with <see langword="using"/></returns>
		public static IDisposable Lock(SemaphoreSlim ss)
		{
			ss.Wait();
			return WeakActionToken.Create(ss, locker => locker.Release());
		}

		/// <summary>
		/// Creates a locker for a slim semaphore asynchronously.
		/// </summary>
		/// <param name="ss">the slim semaphore to acquire async</param>
		/// <returns>the locker to use with <see langword="using"/></returns>
		public static async Task<IDisposable> LockAsync(SemaphoreSlim ss)
		{
			await ss.WaitAsync();
			return WeakActionToken.Create(ss, locker => locker.Release());
		}

		/// <summary>
		/// Creates a locker for a read lock on a <see cref="ReaderWriterLockSlim"/>.
		/// </summary>
		/// <param name="rwl">the lock to acquire in read mode</param>
		/// <returns>the locker to use with <see langword="using"/></returns>
		public static IDisposable LockRead(ReaderWriterLockSlim rwl)
		{
			rwl.EnterReadLock();
			return WeakActionToken.Create(rwl, locker => locker.ExitReadLock());
		}

		/// <summary>
		/// Creates a locker for a write lock <see cref="ReaderWriterLockSlim"/>.
		/// </summary>
		/// <param name="rwl">the lock to acquire in write mode</param>
		/// <returns>the locker to use with <see langword="using"/></returns>
		public static IDisposable LockWrite(ReaderWriterLockSlim rwl)
		{
			rwl.EnterWriteLock();
			return WeakActionToken.Create(rwl, locker => locker.ExitWriteLock());
		}

		/// <summary>
		/// Creates a locker for an upgradable read lock on a <see cref="ReaderWriterLockSlim"/>.
		/// </summary>
		/// <param name="rwl">the lock to acquire in upgradable read mode</param>
		/// <returns>the locker to use with <see langword="using"/></returns>
		public static IDisposable LockReadUpgradable(ReaderWriterLockSlim rwl)
		{
			rwl.EnterUpgradeableReadLock();
			return WeakActionToken.Create(rwl, locker => locker.ExitUpgradeableReadLock());
		}
	}
}