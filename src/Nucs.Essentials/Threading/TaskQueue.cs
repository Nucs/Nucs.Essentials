using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Collections;
using Nucs.Exceptions;
using Nucs.Timing.Bottlenecking;

namespace Nucs.Threading {
    /// <summary>
    ///     A queue that serves as a prioritized task scheduler. The priorities are sorted from small to large.
    /// </summary>
    public class TaskQueue {
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(initialCount: 0, maxCount: int.MaxValue);
        private readonly ConcurrentPriorityQueue<byte, CallTarget> _queue = new ConcurrentPriorityQueue<byte, CallTarget>();

        public Task<T> Enqueue<T>(byte priority, Func<T> act) {
            CallTarget target = new CallTarget(new TaskCompletionSource<object>(), () => act());
            _queue.Enqueue(priority, target);
            _signal.Release(1);
            return target.GetTask<T>();
        }

        public Task<T> Enqueue<T>(Func<T> act) {
            return Enqueue<T>(0, act);
        }

        /*
        public Task<T> Enqueue<T>(byte priority, Func<Task<T>> act) {
            CallTarget target = new CallTarget(new TaskCompletionSource<object>(), act);
            _queue.Enqueue(priority, target);
            _signal.Release(1);
            return target.GetUnboxedTask<T>();
        }

        public Task<T> Enqueue<T>(byte priority, Func<Task> act) {
            CallTarget target = new CallTarget(new TaskCompletionSource<object>(), act);
            _queue.Enqueue(priority, target);
            _signal.Release(1);
            return target.GetUnboxedTask<T>();
        }
        */

        /*
        public Task<T> Enqueue<T>(Func<Task<T>> act) {
            return Enqueue<T>(0, act);
        }
        */

        public Task Enqueue(byte priority, Action act) {
            CallTarget target = new CallTarget(new TaskCompletionSource<object>(), () => {
                act();
                return null;
            });
            _signal.Release(1);
            _queue.Enqueue(priority, target);
            return target.TaskSource.Task;
        }

        public void EnqueueForget<T>(byte priority, Func<T> act) {
            _queue.Enqueue(priority, new CallTarget(null, () => act()));
            _signal.Release(1);
        }

        public void EnqueueForget(byte priority, Action act) {
            _queue.Enqueue(priority, new CallTarget(null, () => {
                act();
                return null;
            }));
            _signal.Release(1);
        }

        /*
        public void EnqueueForget<T>(byte priority, Func<Task<T>> act) {
            _queue.Enqueue(priority, new CallTarget(null, act));
            _signal.Release(1);
        }

        public void EnqueueForget<T>(byte priority, Func<Task> act) {
            _queue.Enqueue(priority, new CallTarget(null, act));
            _signal.Release(1);
        }
        */


        public Task Enqueue(Action act) {
            return Enqueue(0, act);
        }

        public void EnqueueForget<T>(Func<T> act) {
            EnqueueForget<T>(0, act);
        }

        public void EnqueueForget(Action act) {
            EnqueueForget(0, act);
        }

        public async Task<CallTarget> DequeueAsync(CancellationToken token = default) {
            await _signal.WaitAsync(token).ConfigureAwait(false);
            if (!_queue.TryDequeue(out KeyValuePair<byte, CallTarget> target))
                throw new ConcurrencyException("Unexpected concurrency exception");

            return target.Value;
        }

        public async Task ExecuteAsync(CancellationToken token = default) {
            await _signal.WaitAsync(token).ConfigureAwait(false);
            if (!_queue.TryDequeue(out KeyValuePair<byte, CallTarget> target))
                throw new ConcurrencyException("Unexpected concurrency exception");

            if (target.Value.Invoke() is Task task)
                await task.ConfigureAwait(false);
        }

        public async Task<T> ExecuteAsync<T>(CancellationToken token = default) {
            await _signal.WaitAsync(token).ConfigureAwait(false);
            if (!_queue.TryDequeue(out KeyValuePair<byte, CallTarget> target))
                throw new ConcurrencyException("Unexpected concurrency exception");

            object result = target.Value.Invoke();
            if (result is Task<T> task) {
                return await task.ConfigureAwait(false);
            }

            return (T) result;
        }

        public void Execute(CancellationToken token = default) {
            _signal.Wait(token);
            if (!_queue.TryDequeue(out KeyValuePair<byte, CallTarget> target))
                throw new ConcurrencyException("Unexpected concurrency exception");

            if (target.Value.Invoke() is Task task)
                task.GetAwaiter().GetResult();
        }

        public T Execute<T>(CancellationToken token = default) {
            _signal.Wait(token);
            if (!_queue.TryDequeue(out KeyValuePair<byte, CallTarget> target))
                throw new ConcurrencyException("Unexpected concurrency exception");

            object result = target.Value.Invoke();
            if (result is Task<T> task) {
                return task.GetAwaiter().GetResult();
            }

            return (T) result;
        }

        public void Clear() {
            _queue.Clear();
        }

        public int Count => _queue.Count;
    }
}