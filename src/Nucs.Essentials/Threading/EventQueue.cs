using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Nucs.Threading {
    /// <summary>
    ///     A queue that serves as a prioritized task scheduler. The priorities are sorted from small to large.
    /// </summary>
    public class EventQueue<TEvent> where TEvent : Enum {
        private long _listens;
        private readonly ConcurrentDictionary<TEvent, ConcurrentQueue<TaskCompletionSource<object>>> _eventsMap = new ConcurrentDictionary<TEvent, ConcurrentQueue<TaskCompletionSource<object>>>();
        private readonly ConcurrentQueue<TaskCompletionSource<object>> _anyEventsMap = new ConcurrentQueue<TaskCompletionSource<object>>();

        protected Task<object> _waitAsync(TEvent @event) {
            Interlocked.Increment(ref _listens);
            var comp = new TaskCompletionSource<object>();
            _eventsMap.GetOrAdd(@event, eve => new ConcurrentQueue<TaskCompletionSource<object>>())
                      .Enqueue(comp);
            return comp.Task;
        }

        public void Trigger(TEvent @event, object @return) {
            if (_anyEventsMap.Count > 0) {
                while (_anyEventsMap.TryDequeue(out var comp)) {
                    comp.TrySetResult(@return);
                }
            }

            if (Interlocked.Read(ref _listens) != 0 && _eventsMap.TryGetValue(@event, out var queue)) {
                while (queue.TryDequeue(out var handle)) {
                    handle.TrySetResult(@return);
                    Interlocked.Decrement(ref _listens);
                }
            }
        }

        public Task WaitAsync(TEvent @event) {
            return _waitAsync(@event);
        }

        public Task<T> WaitAsync<T>(TEvent @event) {
            return _waitAsync(@event).ContinueWith(t => (T) t.GetAwaiter().GetResult());
        }

        protected Task<object> _waitAnyAsync() {
            Interlocked.Increment(ref _listens);
            var comp = new TaskCompletionSource<object>();
            _anyEventsMap.Enqueue(comp);
            return comp.Task;
        }

        public Task WaitAnyAsync() {
            return _waitAnyAsync();
        }

        public Task<T> WaitAnyAsync<T>() {
            return _waitAnyAsync().ContinueWith(t => (T) t.GetAwaiter().GetResult());
        }

        public void Trigger(TEvent @event) {
            Trigger(@event, null);
        }

        public void Clear() {
            var values = _eventsMap.Values;
            _eventsMap.Clear();
            Interlocked.Exchange(ref _listens, 0L);
            foreach (var queue in values) {
                while (queue.TryDequeue(out var handle)) {
                    handle.TrySetCanceled();
                }
            }
        }

        public int Count => _eventsMap.Count;
    }
}