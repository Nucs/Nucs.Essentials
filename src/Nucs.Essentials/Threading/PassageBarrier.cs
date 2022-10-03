using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nucs.Threading {
    
    /// <summary>
    ///     Requires N amount of threads to reach the barrier (RequestPassage).
    ///     Use RequestFirstPassage untill it returns success==true (when passesRequired has been met) - then inside the if you must call OpenPassage after
    ///     required logic has been handled. after OpenPassage called - all the other threads idling will continue. essentially allowing only the Nth passesRequired thread to act and let all the rest idle.
    /// </summary>
    public class PassageBarrier : IDisposable {
        private TaskCompletionSource _barrier = new TaskCompletionSource();
        private volatile int _passesRequired;
        private int _passes;

        public PassageBarrier(int passesRequired) {
            _passesRequired = passesRequired;
        }

        public bool Completed => _barrier.Task.IsCompletedSuccessfully;
        public TaskCompletionSource Barrier => _barrier;

        public Task RequestPassage(out bool success) {
            var barrier = _barrier;
            if (Interlocked.Increment(ref _passes) == _passesRequired) {
                _barrier = new TaskCompletionSource();
                barrier.TrySetResult();
                success = true;
                return Task.CompletedTask;
            } else
                success = false;

            return barrier.Task;
        }

        public Task RequestFirstPassage(out bool success) {
            var barrier = _barrier;
            if (Interlocked.Increment(ref _passes) == _passesRequired) {
                success = true;
                return Task.CompletedTask;
            } else
                success = false;

            return barrier.Task;
        }

        public bool AskFirstPassage() {
            if (Interlocked.Increment(ref _passes) == _passesRequired) {
                return true;
            }

            return false;
        }

        public void OpenPassage() {
            _barrier.TrySetResult();
        }

        public void Reset(int? requiredPasses = null) {
            Interlocked.Exchange(ref _passes, 0);
            _barrier = new TaskCompletionSource();
            if (requiredPasses.HasValue)
                _passesRequired = requiredPasses.Value;
        }

        public bool DecrementRequiredPasses() {
            if (Interlocked.Decrement(ref _passesRequired) == _passes) {
                _barrier.TrySetResult();
                return true;
            }

            return false;
        }

        public bool IncrementRequiredPasses() {
            if (Interlocked.Increment(ref _passesRequired) == _passes) {
                _barrier.TrySetResult();
                return true;
            }

            return false;
        }

        public bool SetRequiredPasses(int passes) {
            Interlocked.Exchange(ref _passesRequired, passes);
            if (passes == _passes) {
                _barrier.TrySetResult();
                return true;
            }

            return false;
        }

        public readonly struct Empty { }

        #region IDisposable

        public void Dispose() {
            _barrier = default;
        }

        #endregion
    }
}