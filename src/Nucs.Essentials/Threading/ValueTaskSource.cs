using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Extensions.ObjectPool;

namespace Nucs.Threading {
    internal sealed class ValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource {
        #region Pooling

        private static readonly ObjectPool<ValueTaskSource<T>> _pool
            = new DefaultObjectPool<ValueTaskSource<T>>(ValueTaskSourcePooledObjectPolicy.Instance);

        private static readonly ObjectPool<SynchronizationContextPostState> _synchronizationContextPostStatePool
            = new DefaultObjectPool<SynchronizationContextPostState>(
                new DefaultPooledObjectPolicy<SynchronizationContextPostState>());

        private static readonly ObjectPool<ExecutionContextRunState> _executionContextRunStatePool
            = new DefaultObjectPool<ExecutionContextRunState>(
                new DefaultPooledObjectPolicy<ExecutionContextRunState>());

        private sealed class ValueTaskSourcePooledObjectPolicy : IPooledObjectPolicy<ValueTaskSource<T>> {
            public static ValueTaskSourcePooledObjectPolicy Instance { get; }
                = new ValueTaskSourcePooledObjectPolicy();

            private ValueTaskSourcePooledObjectPolicy() { }

            public ValueTaskSource<T> Create() {
                return new ValueTaskSource<T>();
            }

            public bool Return(ValueTaskSource<T> obj) {
                return !obj.Exhausted;
            }
        }

        #endregion

        public Action<object> _continuation;
        public T _result;
        public bool _completed;
        public Exception _exception;
        public object _continuationState;
        public ExecutionContext _executionContext;
        public object _scheduler;

        internal bool Exhausted { get; private set; }
        internal short Token { get; private set; }

        internal static ValueTaskSource<T> Allocate() {
            var result = _pool.Get();
            Debug.Assert(!result.Exhausted);
            Debug.Assert(EqualityComparer<T>.Default.Equals(result._result, default));
            Debug.Assert(result._exception == default);
            Debug.Assert(result._completed == default);
            Debug.Assert(result._continuation == default);
            Debug.Assert(result._continuationState == default);
            Debug.Assert(result._executionContext == default);
            Debug.Assert(result._scheduler == default);
            return result;
        }

        internal bool TryNotifyCompletion(T result, short token) {
            return TrySetCompleted(exception: null, result, token);
        }

        #pragma warning disable CA1068, IDE0060, CA1801
        internal bool TryNotifyCompletion(CancellationToken cancellation, short token)
            #pragma warning restore CA1068, IDE0060, CA1801
        {
            return TrySetCompleted(new TaskCanceledException(), result: default, token);
        }

        internal bool TryNotifyCompletion(Exception exception, short token) {
            Debug.Assert(exception != null);

            return TrySetCompleted(exception, result: default, token);
        }

        private bool TrySetCompleted(Exception exception, T result, short token) {
            Action<object> continuation;
            object continuationState;
            ExecutionContext executionContext;
            object scheduler;

            // Use this object for locking, as this is safe here (internal type) and we do not need to allocate a mutex object.
            lock (this) {
                if (token != Token || _completed) {
                    return false;
                }

                _exception = exception;
                _result = result;
                _completed = true;

                Monitor.PulseAll(this);

                continuation = _continuation;
                continuationState = _continuationState;
                executionContext = _executionContext;
                scheduler = _scheduler;
            }

            ExecuteContinuation(continuation, continuationState, executionContext, scheduler, forceAsync: false);

            return true;
        }

        private void ExecuteContinuation(
            Action<object> continuation,
            object continuationState,
            ExecutionContext executionContext,
            object scheduler,
            bool forceAsync) {
            if (continuation == null)
                return;

            if (executionContext != null) {
                // This case should be relatively rare, as the async Task/ValueTask method builders
                // use the awaiter's UnsafeOnCompleted, so this will only happen with code that
                // explicitly uses the awaiter's OnCompleted instead.

                var executionContextRunState = _executionContextRunStatePool.Get();
                executionContextRunState.ValueTaskSource = this;
                executionContextRunState.Continuation = continuation;
                executionContextRunState.ContinuationState = continuationState;
                executionContextRunState.Scheduler = scheduler;

                static void ExecutionContextCallback(object runState) {
                    var t = (ExecutionContextRunState) runState;
                    try {
                        t.ValueTaskSource.ExecuteContinuation(t.Continuation, t.ContinuationState, executionContext: null, t.Scheduler, forceAsync: false);
                    } finally {
                        _executionContextRunStatePool.Return(t);
                    }
                }

                ExecutionContext.Run(executionContext, ExecutionContextCallback, executionContextRunState);
            } else if (scheduler is SynchronizationContext synchronizationContext) {
                var synchronizationContextPostState = _synchronizationContextPostStatePool.Get();
                synchronizationContextPostState.Continuation = continuation;
                synchronizationContextPostState.ContinuationState = continuationState;

                static void PostCallback(object s) {
                    var t = (SynchronizationContextPostState) s;
                    try {
                        t.Continuation(t.ContinuationState);
                    } finally {
                        _synchronizationContextPostStatePool.Return(t);
                    }
                }

                synchronizationContext.Post(PostCallback, synchronizationContextPostState);
            } else if (scheduler is TaskScheduler taskScheduler) {
                Task.Factory.StartNew(
                    continuation,
                    continuationState,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    taskScheduler);
            } else if (forceAsync) {
                ExecuteContinuation(continuation, continuationState);
            } else {
                Debug.Assert(scheduler is null);

                continuation(continuationState);
            }
        }

        private static void ExecuteContinuation(Action<object> continuation, object continuationState) {
            var synchronizationContext = SynchronizationContext.Current;

            try {
                SynchronizationContext.SetSynchronizationContext(null);

                var threadPoolWorkItem = (WaitCallback) Delegate.CreateDelegate(typeof(WaitCallback), continuation.Target, continuation.Method);
                ThreadPool.QueueUserWorkItem(threadPoolWorkItem, continuationState);
            } finally {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            }
        }

        #region IValueTaskSource

        public ValueTaskSourceStatus GetStatus(short token) {
            bool completed;
            Exception exception;

            // Use this object for locking, as this is safe here (internal type) and we do not need to allocate a mutex object.
            lock (this) {
                if (token != Token) {
                    ThrowMultipleContinuations();
                }

                completed = _completed;
                exception = _exception;
            }

            if (!completed) {
                return ValueTaskSourceStatus.Pending;
            }

            if (exception == null) {
                return ValueTaskSourceStatus.Succeeded;
            }

            if (exception is TaskCanceledException) {
                return ValueTaskSourceStatus.Canceled;
            }

            return ValueTaskSourceStatus.Faulted;
        }


        private static bool TryGetNonDefaultTaskScheduler(out TaskScheduler taskScheduler) {
            taskScheduler = TaskScheduler.Current;

            if (taskScheduler == TaskScheduler.Default) {
                taskScheduler = null;
            }

            return taskScheduler != null;
        }

        private static bool TryGetNonDefaultSynchronizationContext(out SynchronizationContext synchronizationContext) {
            synchronizationContext = SynchronizationContext.Current;

            if (synchronizationContext != null && synchronizationContext.GetType() == typeof(SynchronizationContext)) {
                synchronizationContext = null;
            }

            return synchronizationContext != null;
        }

        private object GetScheduler() {
            if (TryGetNonDefaultSynchronizationContext(out var synchronizationContext)) {
                return synchronizationContext;
            }

            if (TryGetNonDefaultTaskScheduler(out var taskScheduler)) {
                return taskScheduler;
            }

            return null;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) {
            // Use this object for locking, as this is safe here (internal type) and we do not need to allocate a mutex object.
            lock (this) {
                if (token != Token || _continuation != null) {
                    ThrowMultipleContinuations();
                }

                if (!_completed) {
                    if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0) {
                        _executionContext = ExecutionContext.Capture();
                    }

                    if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0) {
                        _scheduler = GetScheduler();
                    }

                    // Remember continuation and state
                    _continuationState = state;
                    _continuation = continuation;
                    return;
                }
            }

            var scheduler = ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0) ? GetScheduler() : null;
            ExecuteContinuation(continuation, state, executionContext: null, scheduler, forceAsync: true);
        }

        public T GetResult(short token) {
            Exception exception;
            T result;

            // Use this object for locking, as this is safe here (internal type) and we do not need to allocate a mutex object.
            lock (this) {
                // If we are not yet completed, block the current thread until we are.
                if (!_completed) {
                    Monitor.Wait(this);
                    Debug.Assert(_completed);
                }

                if (token != Token) {
                    ThrowMultipleContinuations();
                }

                exception = _exception;
                result = _result;

                if (Token == short.MaxValue) {
                    Exhausted = true;
                } else {
                    Token++;
                    _continuation = default;
                    _result = default;
                    _completed = default;
                    _exception = default;
                    _continuationState = default;
                    _executionContext = default;
                    _scheduler = default;
                }
            }

            _pool.Return(this);

            if (exception != null) {
                var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
                exceptionDispatchInfo.Throw();

                Debug.Fail("This must never be reached.");
                throw exception;
            }

            return result;
        }

        void IValueTaskSource.GetResult(short token) {
            GetResult(token);
        }

        #endregion

        private static void ThrowMultipleContinuations() {
            throw new InvalidOperationException("Multiple awaiters are not allowed");
        }

        private sealed class SynchronizationContextPostState {
            public Action<object> Continuation { get; set; }
            public object ContinuationState { get; set; }
        }

        private sealed class ExecutionContextRunState {
            public ValueTaskSource<T> ValueTaskSource { get; set; }
            public Action<object> Continuation { get; set; }
            public object ContinuationState { get; set; }
            public object Scheduler { get; set; }
        }
    }
}