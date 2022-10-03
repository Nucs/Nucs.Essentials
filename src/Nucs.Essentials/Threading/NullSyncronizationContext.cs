using System;
using System.Threading.Tasks;

namespace Nucs.Threading {
    public class NullSyncronizationContext : ISyncronizationContext {
        #region Implementation of IDisposable

        public void Dispose() { }

        #endregion

        #region Implementation of ISyncronizationContext

        public Task<T> Enqueue<T>(Func<T> act) {
            try {
                return Task.FromResult(act());
            } catch (Exception e) {
                return Task.FromException<T>(e);
            }
        }

        public Task Enqueue(Action act) {
            try {
                act();
                return Task.CompletedTask;
            } catch (Exception e) {
                return Task.FromException(e);
            }
        }

        public Task<T> Enqueue<T>(Func<Task<T>> act) {
            try {
                return act();
            } catch (Exception e) {
                return Task.FromException<T>(e);
            }
        }

        public Task Enqueue(Func<Task> act) {
            try {
                return act();
            } catch (Exception e) {
                return Task.FromException(e);
            }
        }

        public void EnqueueForget<T>(Func<T> act) {
            act();
        }

        public void EnqueueForget(Action act) {
            act();
        }

        public void EnqueueForget<T>(Func<Task<T>> act) {
            _ = act();
        }

        public void EnqueueForget(Func<Task> act) {
            _ = act();
        }

        #endregion
    }
}