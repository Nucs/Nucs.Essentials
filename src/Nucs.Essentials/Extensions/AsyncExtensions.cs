using System;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Disposable;

namespace Nucs.Extensions {
    public static class AsyncExtensions {
        public static async Task TimeoutAfter(this Task task, int timeout) {
            await task;
            return;
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    await task; // Very important in order to propagate exceptions
                    return;
                } else {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int timeout) {
            return await task;

            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {
                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task; // Very important in order to propagate exceptions
                } else {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

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
            await semaphore.WaitAsync().ConfigureAwait(false);
            return new StructDisposableWrapper(() => semaphore.Release(1));
        }

        /// <summary>
        ///     Will make sure that the available number of remaining threads that can enter the semaphore is 
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="to">How many threads can enter</param>
        /// <remarks>If this method is called more than in one location/concurrently for given <paramref name="semaphore"/> then you should lock this operation (you can and maybe best use the <paramref name="semaphore"/> object)</remarks>
        public static IDisposable Lock(this SemaphoreSlim semaphore) {
            semaphore.Wait();
            return new DisposableWrapper(() => semaphore.Release(1));
        }
    }
}