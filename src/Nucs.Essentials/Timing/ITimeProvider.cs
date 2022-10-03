using System;

namespace Nucs.Timing {
    public interface ITimeProvider {
        long TicksNow { get; }
        DateTime Now { get; }
        DateTime NowUTC { get; }
        DateTime Today { get; }

        void Sync(DateTime dt);
    }
}