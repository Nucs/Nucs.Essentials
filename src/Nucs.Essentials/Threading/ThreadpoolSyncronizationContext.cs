using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucs.Threading {
    public class ThreadpoolSyncronizationContext : ISyncronizationContext {
        public void Dispose() { }

        public Task<T> Enqueue<T>(Func<Task<T>> act) {
            return Task.Run(act);
        }

        public Task Enqueue(Func<Task> act) {
            return Task.Run(act);
        }

        public void EnqueueForget<T>(Func<Task<T>> act) {
            _ = Task.Run(act);
        }

        public void EnqueueForget(Func<Task> act) {
            _ = Task.Run(act);
        }

        public Task<T> Enqueue<T>(Func<T> act) {
            return Task.Run(act);
        }

        public Task Enqueue(Action act) {
            return Task.Run(act);
        }

        public void EnqueueForget<T>(Func<T> act) {
            _ = ThreadPool.QueueUserWorkItem(state => ((Func<T>) state)(), act);
        }

        public void EnqueueForget(Action act) {
            _ = ThreadPool.QueueUserWorkItem(state => ((Action) state)(), act);
        }
    }
}