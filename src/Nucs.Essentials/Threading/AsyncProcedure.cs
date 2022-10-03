using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Extensions;

namespace Nucs.Threading {
    [Serializable]
    public class ProcedureFailedException : Exception {
        public ProcedureFailedException() { }
        public ProcedureFailedException(string message) : base(message) { }
        public ProcedureFailedException(string message, Exception inner) : base(message, inner) { }

        protected ProcedureFailedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }

    public enum ProcedureState : byte {
        Uninitialized,
        Running,
        Completed,
        Failed,
    }

    /// <summary>
    ///     Represents an async (via Task{T}) procedure that has to happen every N time, can fail and can retry.
    ///     A procedure can be long-running or just a piece of code that has to complete successfuly (thus retry).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class AsyncProcedure<T> : IDisposable {
        public readonly Func<Task<T>> Procedure;
        public readonly CancellationToken CancellationToken;
        private readonly int _allowedAttempts;
        private Task? _coreRunner;
        private TaskCompletionSource<T> _completionSource;
        private readonly SemaphoreSlim _retrySignal;
        private volatile ProcedureState _state = ProcedureState.Uninitialized;

        public ProcedureState State {
            get => _state;
            set {
                if ((byte) _state == (byte) value)
                    return;
                _state = value;
            }
        }

        public AsyncProcedure(Func<Task<T>> procedure, CancellationToken cancellationToken = default, bool startProcedure = false, int allowedAttempts = 1) {
            Procedure = procedure ?? throw new ArgumentNullException(nameof(procedure));
            CancellationToken = cancellationToken;
            _completionSource = new TaskCompletionSource<T>();
            allowedAttempts = Math.Max(1, allowedAttempts);
            _allowedAttempts = allowedAttempts;
            _retrySignal = new SemaphoreSlim(allowedAttempts, allowedAttempts);

            if (startProcedure)
                _coreRunner = Task.Run(ProcedureRunner, cancellationToken);
        }

        public void ResetState() {
            var toComplete = _completionSource;
            if (State != ProcedureState.Uninitialized || toComplete.Task.IsCompleted) {
                State = ProcedureState.Uninitialized;
                if (!toComplete.Task.IsCompleted) {
                    _completionSource = new TaskCompletionSource<T>();
                    toComplete.TrySetCanceled();
                } else {
                    State = ProcedureState.Uninitialized;
                    _completionSource = new TaskCompletionSource<T>();
                }
            }
        }

        public void RunProcedure() {
            _retrySignal.ReleaseTo(_allowedAttempts);

            if (State is ProcedureState.Completed or ProcedureState.Failed) {
                ResetState();
            }

            _coreRunner ??= Task.Run(ProcedureRunner, CancellationToken);
        }

        private async Task ProcedureRunner() {
            CancellationToken? source = default == CancellationToken ? null : CancellationToken;
            while (source?.IsCancellationRequested != true) {
                _rerun:
                try {
                    State = ProcedureState.Running;
                    var res = await Procedure().ConfigureAwait(false);
                    State = ProcedureState.Completed;
                    //consume all waits
                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                    while (_retrySignal.Wait(0)) { }

                    _completionSource.TrySetResult(res);
                    await _retrySignal.WaitAsync(CancellationToken).ConfigureAwait(false);
                } catch (Exception e) {
                    if (e is not ProcedureFailedException)
                        SystemHelper.Logger?.Error(e.ToString());

                    if (_retrySignal.CurrentCount == 0 || e is ProcedureFailedException) {
                        //notify all awaiters that we have failed retrieving it.
                        State = ProcedureState.Failed;
                        var toFail = _completionSource;
                        _completionSource = new TaskCompletionSource<T>();
                        toFail.TrySetException(e);
                    }

                    await _retrySignal.WaitAsync(CancellationToken).ConfigureAwait(false);
                    goto _rerun;
                }
            }
        }

        /// <summary>
        ///     Returns a Task that awaits this procedure completion.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Task<T> EnsureCompleted() {
            _coreRunner ??= Task.Run(ProcedureRunner, CancellationToken);

            switch (State) {
                case ProcedureState.Completed:
                    return _completionSource.Task;
                case ProcedureState.Running:
                    return AwaitTask();
                case ProcedureState.Uninitialized or ProcedureState.Failed:
                    RunProcedure();
                    return AwaitTask();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async ValueTask<T> Await() {
            if (_completionSource.Task.IsCompletedSuccessfully)
                return _completionSource.Task.GetAwaiter().GetResult(); //unbox

            T res;
            do {
                _rewait:
                try {
                    res = await _completionSource.Task.ConfigureAwait(false);
                } catch (TaskCanceledException) {
                    if (_completionSource.Task.IsCanceled)
                        throw;
                    //we have a new awaiter
                    goto _rewait;
                }
            } while (!_completionSource.Task.IsCompletedSuccessfully || State != ProcedureState.Completed);

            return res;
        }

        public async Task<T> AwaitTask() {
            if (_completionSource.Task.IsCompletedSuccessfully)
                return _completionSource.Task.GetAwaiter().GetResult(); //unbox

            T res;
            do {
                _rewait:
                try {
                    res = await _completionSource.Task.ConfigureAwait(false);
                } catch (TaskCanceledException) {
                    if (_completionSource.Task.IsCanceled)
                        throw;
                    //we have a new awaiter
                    goto _rewait;
                }
            } while (!_completionSource.Task.IsCompletedSuccessfully || State != ProcedureState.Completed);

            return res;
        }

        public TaskAwaiter<T> GetAwaiter() {
            return AwaitTask().GetAwaiter();
        }

        public void Dispose() {
            _completionSource?.TrySetException(new ObjectDisposedException(Procedure.ToString()));
            _retrySignal?.Dispose();
        }
    }
}