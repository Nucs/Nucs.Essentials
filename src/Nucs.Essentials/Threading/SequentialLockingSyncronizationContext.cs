using System;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Extensions;

namespace Nucs.Threading {
    public class SequentialLockingSyncronizationContext : ISyncronizationContext {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        #region Implementation of IDisposable

        public void Dispose() {
            _semaphore.SafeDispose();
        }

        #endregion

        #region Implementation of ISyncronizationContext

        public Task<T> Enqueue<T>(Func<T> act) {
            _semaphore.Wait();
            try {
                return Task.FromResult(act());
            } catch (Exception e) {
                return Task.FromException<T>(e);
            } finally {
                _semaphore.Release(1);
            }
        }

        public Task Enqueue(Action act) {
            _semaphore.Wait();
            try {
                act();
                return Task.CompletedTask;
            } catch (Exception e) {
                return Task.FromException(e);
            } finally {
                _semaphore.Release(1);
            }
        }

        public Task<T> Enqueue<T>(Func<Task<T>> act) {
            _semaphore.Wait();
            try {
                return act().ContinueWith(task => {
                    _semaphore.Release(1);
                    return task.GetAwaiter().GetResult();
                });
            } catch (Exception e) {
                _semaphore.Release(1);
                return Task.FromException<T>(e);
            }
        }

        public Task Enqueue(Func<Task> act) {
            _semaphore.Wait();
            try {
                return act().ContinueWith(task => {
                    _semaphore.Release(1);
                    task.GetAwaiter().GetResult();
                });
            } catch (Exception e) {
                _semaphore.Release(1);
                return Task.FromException(e);
            }
        }

        public void EnqueueForget<T>(Func<T> act) {
            _semaphore.Wait();
            try {
                act();
            } finally {
                _semaphore.Release(1);
            }
        }

        public void EnqueueForget(Action act) {
            _semaphore.Wait();
            try {
                act();
            } finally {
                _semaphore.Release(1);
            }
        }

        public void EnqueueForget<T>(Func<Task<T>> act) {
            _semaphore.Wait();
            try {
                act().ContinueWith(task => {
                    _semaphore.Release(1);
                    task.GetAwaiter().GetResult();
                });
            } catch (Exception e) {
                _semaphore.Release(1);
                throw;
            }
        }

        public void EnqueueForget(Func<Task> act) {
            _semaphore.Wait();
            try {
                act().ContinueWith(task => {
                    _semaphore.Release(1);
                    task.GetAwaiter().GetResult();
                });
            } catch (Exception e) {
                _semaphore.Release(1);
                throw;
            }
        }

        #endregion
    }
}