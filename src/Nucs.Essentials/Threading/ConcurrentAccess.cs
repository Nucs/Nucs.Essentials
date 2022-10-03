using System;
using System.Diagnostics;
using System.Threading;

namespace Nucs.Threading {
    /// <summary>
    ///     Wraps <see cref="ReaderWriterLockSlim"/> to a more friendly access.
    /// </summary>
    [DebuggerStepThrough]
    public class ConcurrentAccess : IConcurrentAccess {
        private readonly ReaderWriterLockSlim _rw = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public ReaderWriterLockSlim ReaderWriterLock {
            [DebuggerStepThrough] get { return _rw; }
        }

        public IDisposable RequestAccess() {
            _rw.EnterReadLock();
            return new DisposeAction(() => { _rw.ExitReadLock(); });
        }

        public IDemandableWriteAccess RequestUpgradableAccess() {
            _rw.EnterUpgradeableReadLock();
            _rw.EnterReadLock();
            return new DemandableWriteAccess(_rw);
        }

        public IDisposable RequestWrite() {
            _rw.EnterWriteLock();

            return new DisposeAction(() => { _rw.ExitWriteLock(); });
        }

        public bool TryRequestUpgradableAccess(int milliseconds, out IDemandableWriteAccess @out) {
            return TryRequestUpgradableAccess(TimeSpan.FromMilliseconds(milliseconds), out @out);
        }

        public bool TryRequestUpgradableAccess(TimeSpan ts, out IDemandableWriteAccess @out) {
            if (!_rw.TryEnterWriteLock(ts)) {
                @out = null;
                return false;
            }

            @out = new DemandableWriteAccess(_rw);
            return true;
        }

        public bool TryRequestWrite(int milliseconds, out IDisposable @out) {
            return TryRequestWrite(TimeSpan.FromMilliseconds(milliseconds), out @out);
        }

        public bool TryRequestWrite(TimeSpan ts, out IDisposable @out) {
            if (!_rw.TryEnterWriteLock(ts)) {
                @out = null;
                return false;
            }

            @out = new DisposeAction(() => { _rw.ExitWriteLock(); });
            return true;
        }

        public bool TryRequestAccess(int milliseconds, out IDisposable @out) {
            return TryRequestAccess(TimeSpan.FromMilliseconds(milliseconds), out @out);
        }

        public bool TryRequestAccess(TimeSpan ts, out IDisposable @out) {
            if (!_rw.TryEnterReadLock(ts)) {
                @out = null;
                return false;
            }

            @out = new DisposeAction(() => { _rw.ExitReadLock(); });
            return true;
        }

        [DebuggerStepThrough]
        internal class DisposeAction : IDisposable {
            private readonly Action _action;

            public DisposeAction(Action action) {
                _action = action;
            }

            /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
            public void Dispose() {
                _action();
            }
        }

        public void Dispose() {
            _rw.Dispose();
        }
    }
}