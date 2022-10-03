using System;

namespace Nucs.Threading {
    public interface IConcurrentAccess : IDisposable {
        IDisposable RequestAccess();
        IDemandableWriteAccess RequestUpgradableAccess();
        IDisposable RequestWrite();
        bool TryRequestUpgradableAccess(int milliseconds, out IDemandableWriteAccess @out);
        bool TryRequestUpgradableAccess(TimeSpan ts, out IDemandableWriteAccess @out);
        bool TryRequestWrite(int milliseconds, out IDisposable @out);
        bool TryRequestWrite(TimeSpan ts, out IDisposable @out);
        bool TryRequestAccess(int milliseconds, out IDisposable @out);
        bool TryRequestAccess(TimeSpan ts, out IDisposable @out);
    }
}