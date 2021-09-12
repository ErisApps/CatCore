using System;
using System.Threading;

namespace CatCore.Helpers
{
	/// <summary>
    /// Convenience class for dealing with randomness.
    /// </summary>
    /// <remarks>
    /// Adapted from https://codeblog.jonskeet.uk/2009/11/04/revisiting-randomness/
    /// </remarks>
    internal sealed class ThreadSafeRandomFactory
    {
        /// <summary>
        /// Random number generator used to generate seeds,
        /// which are then used to create new random number
        /// generators on a per-thread basis.
        /// </summary>
        private readonly Random _globalRandom;
        private readonly SemaphoreSlim _globalLock;

        /// <summary>
        /// ThreadLocal random number generator
        /// </summary>
        private readonly ThreadLocal<Random> _threadRandom;

        public ThreadSafeRandomFactory()
        {
	        _globalLock = new SemaphoreSlim(1, 1);
	        _globalRandom = new Random();

	        _threadRandom = new ThreadLocal<Random>(CreateNewRandom);
        }

        /// <summary>
        /// Creates a new instance of Random. The seed is derived from a global (static) instance of Random, rather than time.
        /// </summary>
        public Random CreateNewRandom()
        {
	        using var _ = Synchronization.Lock(_globalLock);
	        return new Random(_globalRandom.Next());
        }

        /// <summary>
        /// Returns an instance of Random which can be used freely within the current thread.
        /// </summary>
        public Random Instance => _threadRandom.Value;
    }
}