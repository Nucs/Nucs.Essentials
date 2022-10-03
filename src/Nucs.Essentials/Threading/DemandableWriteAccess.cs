using System.Diagnostics;
using System.Threading;

namespace Nucs.Threading {
    /// <summary>
    ///     Gives an option to upgrade to write access, do not forget to dispose.
    /// </summary>
    [DebuggerStepThrough]
    public class DemandableWriteAccess : IDemandableWriteAccess {
        private readonly ReaderWriterLockSlim _rw;

        public DemandableWriteAccess(ReaderWriterLockSlim rw) {
            _rw = rw;
        }

        public void Demand() {
            if (!_rw.IsWriteLockHeld)
                _rw.EnterWriteLock();
        }

        public void Free() {
            if (_rw.IsWriteLockHeld)
                _rw.ExitWriteLock();
        }

        public void Dispose() {
            if (_rw.IsWriteLockHeld)
                _rw.ExitWriteLock();
            _rw.ExitReadLock();
            _rw.ExitUpgradeableReadLock();
        }
    }
}