using System;

namespace Nucs.Threading {
    /// <summary>
    ///     When <see cref="IConcurrentAccess"/> is requried but no locking actually needed.
    /// </summary>
    /// <remarks>Does nothing.</remarks>
    public class DullConcurrentAccess : IConcurrentAccess {
        public static readonly DullConcurrentAccess Instance = new DullConcurrentAccess();

        private DullConcurrentAccess() { }

        public IDisposable RequestAccess() {
            return EmptyDisposeable.Singeton;
        }

        public IDemandableWriteAccess RequestUpgradableAccess() {
            return EmptyDemandableWriteAccess.Singeton;
        }

        public IDisposable RequestWrite() {
            return EmptyDisposeable.Singeton;
        }

        public bool TryRequestUpgradableAccess(int milliseconds, out IDemandableWriteAccess @out) {
            @out = EmptyDemandableWriteAccess.Singeton;
            return true;
        }

        public bool TryRequestUpgradableAccess(TimeSpan ts, out IDemandableWriteAccess @out) {
            @out = EmptyDemandableWriteAccess.Singeton;
            return true;
        }

        public bool TryRequestWrite(int milliseconds, out IDisposable @out) {
            @out = EmptyDisposeable.Singeton;
            return true;
        }

        public bool TryRequestWrite(TimeSpan ts, out IDisposable @out) {
            @out = EmptyDisposeable.Singeton;
            return true;
        }

        public bool TryRequestAccess(int milliseconds, out IDisposable @out) {
            @out = EmptyDisposeable.Singeton;
            return true;
        }

        public bool TryRequestAccess(TimeSpan ts, out IDisposable @out) {
            @out = EmptyDisposeable.Singeton;
            return true;
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose() { }

        private class EmptyDisposeable : IDisposable {
            public static readonly EmptyDisposeable Singeton = new EmptyDisposeable();
            private EmptyDisposeable() { }
            public void Dispose() { }
        }

        private class EmptyDemandableWriteAccess : IDemandableWriteAccess {
            public static readonly EmptyDemandableWriteAccess Singeton = new EmptyDemandableWriteAccess();
            private EmptyDemandableWriteAccess() { }
            public void Dispose() { }
            public void Free() { }
            public void Demand() { }
        }
    }
}