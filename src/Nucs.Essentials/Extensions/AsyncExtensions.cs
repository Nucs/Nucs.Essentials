using System;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Disposable;

namespace Nucs.Extensions {
    public static class AsyncExtensions {

        /// <summary>
        ///     Will make sure that the available number of remaining threads that can enter the semaphore is 
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="to">How many threads can enter</param>
        /// <remarks>If this method is called more than in one location/concurrently for given <paramref name="semaphore"/> then you should lock this operation (you can and maybe best use the <paramref name="semaphore"/> object)</remarks>
        public static void ReleaseTo(this SemaphoreSlim semaphore, int to) {
            while (true) {
                try {
                    var diff = to - semaphore.CurrentCount;
                    if (diff > 0)
                        semaphore.Release(diff);
                    break;
                } catch (SemaphoreFullException) { }
            }
        }

        /// <summary>
        ///     Will make sure that the available number of remaining threads that can enter the semaphore is 
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="to">How many threads can enter</param>
        /// <remarks>If this method is called more than in one location/concurrently for given <paramref name="semaphore"/> then you should lock this operation (you can and maybe best use the <paramref name="semaphore"/> object)</remarks>
        public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphore) {
            await semaphore.WaitAsync().ConfigureAwait(false);
            return new DisposableWrapper(() => semaphore.Release(1));
        }

        /// <summary>
        ///     Will make sure that the available number of remaining threads that can enter the semaphore is 
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="to">How many threads can enter</param>
        /// <remarks>If this method is called more than in one location/concurrently for given <paramref name="semaphore"/> then you should lock this operation (you can and maybe best use the <paramref name="semaphore"/> object)</remarks>
        public static async ValueTask<StructDisposableWrapper> LockAsyncStruct(this SemaphoreSlim semaphore) {
            if (!semaphore.Wait(0, default))
                await semaphore.WaitAsync().ConfigureAwait(false);
            return new StructDisposableWrapper(() => semaphore.Release(1));
        }

        /// <summary>
        ///     Will make sure that the available number of remaining threads that can enter the semaphore is 
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="to">How many threads can enter</param>
        /// <remarks>If this method is called more than in one location/concurrently for given <paramref name="semaphore"/> then you should lock this operation (you can and maybe best use the <paramref name="semaphore"/> object)</remarks>
        public static DisposableWrapper Lock(this SemaphoreSlim semaphore) {
            semaphore.Wait();
            return new DisposableWrapper(() => semaphore.Release(1));
        }
    }
}