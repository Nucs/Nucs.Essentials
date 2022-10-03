using System;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Extensions;
using Nucs.Timing.Bottlenecking;

namespace Nucs.Threading {
    public class TaskQueueWorker : IDisposable {
        private readonly CancellationTokenSource _threadCancel = new CancellationTokenSource();
        private readonly TaskQueue Queue;

        public TaskQueueWorker(TaskQueue queue) {
            Queue = queue;
            _ = Task.Run(() => TaskQueueWorkerContext(_threadCancel.Token));
        }

        public TaskQueueWorker() : this(new TaskQueue()) { }

        public async Task TaskQueueWorkerContext(CancellationToken token) {
            while (!token.IsCancellationRequested)
                await Queue.ExecuteAsync(token).ConfigureAwait(false);
        }

        public void Clear() {
            Queue.Clear();
        }

        public int Count => Queue.Count;

        public Task<CallTarget> DequeueAsync(CancellationToken token = default) {
            return Queue.DequeueAsync(token);
        }

        public Task<T> Enqueue<T>(byte priority, Func<T> act) {
            return Queue.Enqueue(priority, act);
        }

        public Task<T> Enqueue<T>(Func<T> act) {
            return Queue.Enqueue(act);
        }

        public Task Enqueue(byte priority, Action act) {
            return Queue.Enqueue(priority, act);
        }

        public Task Enqueue(Action act) {
            return Queue.Enqueue(act);
        }

        public void EnqueueForget<T>(byte priority, Func<T> act) {
            Queue.EnqueueForget(priority, act);
        }

        public void EnqueueForget(byte priority, Action act) {
            Queue.EnqueueForget(priority, act);
        }

        public void EnqueueForget<T>(Func<T> act) {
            Queue.EnqueueForget(act);
        }

        public void EnqueueForget(Action act) {
            Queue.EnqueueForget(act);
        }

        public Task ExecuteAsync(CancellationToken token = default) {
            return Queue.ExecuteAsync(token);
        }

        public Task<T> ExecuteAsync<T>(CancellationToken token = default) {
            return Queue.ExecuteAsync<T>(token);
        }


        #region IDisposable

        public void Dispose() {
            _threadCancel.SafeCancelAndDispose();
        }

        #endregion
    }
}