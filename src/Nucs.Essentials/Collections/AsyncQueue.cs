using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Collections.Structs;

namespace Nucs.Collections {
    public class AsyncQueue<T> {
        private StructQueue<T> Queue;
        public readonly object QueueLock = new object();
        public readonly SemaphoreSlim Signal = new SemaphoreSlim(initialCount: 0, maxCount: int.MaxValue);

        public AsyncQueue() {
            Queue = new StructQueue<T>(0);
        }

        public AsyncQueue(ConcurrentQueue<T> concurrentQueue) {
            Queue = new StructQueue<T>(concurrentQueue);
        }

        public async ValueTask<T> DequeueAsync() {
            await Signal.WaitAsync(Timeout.Infinite, CancellationToken.None).ConfigureAwait(false);
            lock (QueueLock)
                return Queue.Dequeue();
        }

        public T Peak() {
            lock (QueueLock)
                return Queue.Peek();
        }

        public bool TryDequeue(out T @out) {
            if (Signal.Wait(0)) {
                lock (QueueLock)
                    @out = Queue.Dequeue();
                return true;
            }

            @out = default;
            return false;
        }

        public async ValueTask<T> DequeueAsync(CancellationToken token) {
            await Signal.WaitAsync(token).ConfigureAwait(false);
            lock (QueueLock)
                return Queue.Dequeue();
        }

        public void Enqueue(T value) {
            lock (QueueLock) {
                Queue.Enqueue(ref value);
                Signal.Release(1);
            }
        }

        public void Enqueue(T value, T value2) {
            lock (QueueLock) {
                Queue.Enqueue(ref value);
                Queue.Enqueue(ref value2);
                Signal.Release(2);
            }
        }

        public void Enqueue(T value, T value2, T value3) {
            lock (QueueLock) {
                Queue.Enqueue(ref value);
                Queue.Enqueue(ref value2);
                Queue.Enqueue(ref value3);
                Signal.Release(3);
            }
        }

        public void Enqueue(ref T value) {
            lock (QueueLock) {
                Queue.Enqueue(ref value);
                Signal.Release(1);
            }
        }

        public void Enqueue(ref T value, ref T value2) {
            lock (QueueLock) {
                Queue.Enqueue(ref value);
                Queue.Enqueue(ref value2);
                Signal.Release(2);
            }
        }

        public void Enqueue(ref T value, ref T value2, ref T value3) {
            lock (QueueLock) {
                Queue.Enqueue(ref value);
                Queue.Enqueue(ref value2);
                Queue.Enqueue(ref value3);
                Signal.Release(3);
            }
        }

        public void EnqueueRange(IList<T> values) {
            lock (QueueLock) {
                int i;
                for (i = 0; i < values.Count; i++) {
                    Queue.Enqueue(values[i]);
                }

                Signal.Release(i);
            }
        }

        public void EnqueueRange(List<T> values) {
            lock (QueueLock) {
                int i;
                for (i = 0; i < values.Count; i++) {
                    Queue.Enqueue(values[i]);
                }

                Signal.Release(i);
            }
        }

        public void EnqueueRange(IEnumerable<T> values) {
            lock (QueueLock) {
                int count = 0;
                foreach (var value in values) {
                    Queue.Enqueue(value);
                    count++;
                }

                Signal.Release(count);
            }
        }

        public void Clear() {
            lock (QueueLock)
                Queue.Clear();
        }

        public int Count {
            get {
                lock (QueueLock)
                    return Queue.Count;
            }
        }

        public T[] ToArray() {
            lock (QueueLock)
                return Queue.ToArray();
        }

        public void Trim() {
            lock (QueueLock)
                Queue.TrimExcess();
        }
    }
}