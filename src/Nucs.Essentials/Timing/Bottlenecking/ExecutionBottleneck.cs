using System;
using System.Collections.Concurrent;
using System.Threading;
using Timer = System.Timers.Timer;

namespace Nucs.Timing.Bottlenecking {
    public sealed class ExecutionBottleneck<TParameters> : IDisposable {
        private readonly Action<TParameters> _callback;
        private readonly int _executionsPerInterval;
        private readonly ConcurrentQueue<TParameters> _queue = new ConcurrentQueue<TParameters>();
        private bool _processingQueue;
        private readonly object _processingQueueLock = new object();
        private int _executionsSinceIntervalStart;
        private readonly Timer _timer;
        private bool _disposed;

        public ExecutionBottleneck(Action<TParameters> callback, TimeSpan interval, int executionsPerInterval) {
            _callback = callback;
            _executionsPerInterval = executionsPerInterval;
            _timer = new Timer(interval.TotalMilliseconds);
            _timer.AutoReset = true;
            _timer.Start();
            _timer.Elapsed += OnIntervalEnd;
        }

        public void Enqueue(TParameters parameters) {
            _queue.Enqueue(parameters);
        }

        private void TryProcessQueue() {
            if (_processingQueue) return;
            lock (_processingQueueLock) {
                if (_processingQueue) return;
                _processingQueue = true;
                try {
                    ProcessQueue();
                } finally {
                    _processingQueue = false;
                }
            }
        }

        private void ProcessQueue() {
            // ReSharper disable once InlineOutVariableDeclaration
            TParameters dequeuedParameters;
            while ((_executionsSinceIntervalStart < _executionsPerInterval) && _queue.TryDequeue(out dequeuedParameters)) {
                Interlocked.Increment(ref _executionsSinceIntervalStart);
                _callback.Invoke(dequeuedParameters);
            }
        }


        private void OnIntervalEnd(object sender, System.Timers.ElapsedEventArgs e) {
            _executionsSinceIntervalStart = 0;
            TryProcessQueue();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ExecutionBottleneck() {
            Dispose(false);
        }

        private void Dispose(bool disposing) {
            if (_disposed)
                return;

            if (disposing) {
                _timer.Dispose();
            }

            _disposed = true;
        }
    }
}