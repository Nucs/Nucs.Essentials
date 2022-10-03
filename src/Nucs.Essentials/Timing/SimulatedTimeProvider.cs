using System;
using System.Runtime.CompilerServices;

namespace Nucs.Timing {
    public struct SimulatedTimeProvider : ITimeProvider {
        private long _now;

        public SimulatedTimeProvider(DateTime now) {
            _now = now.Ticks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Step(TimeSpan ts) {
            _now += ts.Ticks;
        }

        public readonly long TicksNow {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _now;
        }

        public readonly DateTime Today {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new DateTime(_now - _now % 864000000000L);
        }

        public readonly DateTime TodayUTC {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new DateTime(_now - _now % 864000000000L, DateTimeKind.Utc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sync(DateTime ts) {
            _now = ts.Ticks;
        }

        public readonly DateTime Now {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new DateTime(_now);
        }

        public readonly DateTime NowUTC {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => new DateTime(_now, DateTimeKind.Utc);
        }
    }
}