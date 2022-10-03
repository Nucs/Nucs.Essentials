using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Threading;
using Timer = System.Timers.Timer;

namespace Nucs.Timing.Bottlenecking {
    public sealed class ExecutionTimer : ISyncronizationContext {
        private readonly int _executionsPerInterval;
        private readonly ConcurrentQueue<CallTarget> _queue = new ConcurrentQueue<CallTarget>();
        private readonly int _skipWaitingInterval;
        private readonly object _processingQueueLock = new object();
        private readonly object _skippingLock = new object();
        private bool _processingQueue;
        private int _executionsSinceIntervalStart;
        private readonly Timer _timer;
        private bool _disposed;
        private bool _skipped;

        /// <summary>
        ///     If a step was triggered but nothing fired then we can execute immediately instead of enqueuing
        /// </summary>
        public bool AllowImmediateCall { get; set; } //TODO: Eli: .net5 init

        public ExecutionTimer(TimeSpan interval, int executionsPerInterval) {
            _executionsPerInterval = executionsPerInterval;
            _skipWaitingInterval = (int) interval.TotalMilliseconds / 2;
            _timer = new Timer(interval.TotalMilliseconds);

            _timer.AutoReset = true;
            _timer.Start();
            _timer.Elapsed += OnIntervalEnd;
        }

        public Task<T> Enqueue<T>(Func<T> act) {
            //if (_skipped && _queue.IsEmpty && Monitor.TryEnter(_processingQueueLock, _skipWaitingInterval)) {
            //    try {
            //        if (_skipped) {
            //            _skipped = false;
            //            return Task.FromResult(act());
            //        }
            //    } finally {
            //        Monitor.Exit(_processingQueueLock);
            //    }
            //}

            var target = new CallTarget(new TaskCompletionSource<object>(), () => act());
            _queue.Enqueue(target);
            return target.GetTask<T>();
        }

        public Task Enqueue(Action act) {
            //if (_skipped && _queue.IsEmpty && Monitor.TryEnter(_processingQueueLock, _skipWaitingInterval)) {
            //    try {
            //        if (_skipped) {
            //            _skipped = false;
            //            act();
            //            return Task.CompletedTask;
            //        }
            //    } finally {
            //        Monitor.Exit(_processingQueueLock);
            //    }

            //}

            var target = new CallTarget(new TaskCompletionSource<object>(), () => {
                act();
                return null;
            });
            _queue.Enqueue(target);
            return target.TaskSource.Task;
        }

        public Task<T> Enqueue<T>(Func<Task<T>> act) {
            var target = new CallTarget(new TaskCompletionSource<object>(), act);
            _queue.Enqueue(target);
            return target.GetUnboxedTask<T>();
        }

        public Task Enqueue(Func<Task> act) {
            var target = new CallTarget(new TaskCompletionSource<object>(), act);
            _queue.Enqueue(target);
            return target.GetUnboxedTask();
        }

        public void EnqueueForget<T>(Func<T> act) {
            //if (_skipped && _queue.IsEmpty && Monitor.TryEnter(_processingQueueLock, _skipWaitingInterval)) {
            //    try {
            //        if (_skipped) {
            //            _skipped = false;
            //            act();
            //            return;
            //        }
            //    } finally {
            //        Monitor.Exit(_processingQueueLock);
            //    }
            //}

            _queue.Enqueue(new CallTarget(null, () => act()));
        }

        public void EnqueueForget(Action act) {
            //if (_skipped && _queue.IsEmpty && Monitor.TryEnter(_processingQueueLock, _skipWaitingInterval)) {
            //    try {
            //        if (_skipped) {
            //            _skipped = false;
            //            act();
            //            return;
            //        }
            //    } finally {
            //        Monitor.Exit(_processingQueueLock);
            //    }
            //}

            _queue.Enqueue(new CallTarget(null, () => {
                act();
                return null;
            }));
        }

        public void EnqueueForget<T>(Func<Task<T>> act) {
            var target = new CallTarget(new TaskCompletionSource<object>(), act);
            _queue.Enqueue(target);
        }

        public void EnqueueForget(Func<Task> act) {
            var target = new CallTarget(new TaskCompletionSource<object>(), act);
            _queue.Enqueue(target);
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
            CallTarget dequeuedParameters;
            if (!_queue.TryDequeue(out dequeuedParameters)) {
                if (AllowImmediateCall)
                    _skipped = true;
                return;
            }

            do {
                Interlocked.Increment(ref _executionsSinceIntervalStart);
                dequeuedParameters.Invoke();
            } while ((_executionsSinceIntervalStart < _executionsPerInterval) && _queue.TryDequeue(out dequeuedParameters));
        }

        private void OnIntervalEnd(object sender, System.Timers.ElapsedEventArgs e) {
            _executionsSinceIntervalStart = 0;
            TryProcessQueue();
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ExecutionTimer() {
            Dispose(false);
        }

        private void Dispose(bool disposing) {
            if (_disposed)
                return;

            if (disposing)
                _timer.Dispose();

            _disposed = true;
        }
    }
}