using System;

namespace Nucs.Threading {
    public interface IDemandableWriteAccess : IDisposable {
        /// <summary>
        ///     Enters write lock.
        /// </summary>
        void Demand();

        /// <summary>
        ///     Exits write lock, note that it is also performed when <see cref="IDisposable.Dispose"/>ing.
        /// </summary>
        void Free();
    }
}