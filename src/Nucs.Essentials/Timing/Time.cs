using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Nucs.Collections;

namespace Nucs.Timing {
    /** 
     * Utility class that handles the time aspect of the strategy.
     * If the strategy is fed by market data, this utility will be set as simulation mode.
     * In simulation mode, the time is determined by the market data.  In live mode,
     * the time is determined by the windows time.
     */
    public static class Time {
        /// <summary>
        ///     A concurrent dictionary of taking in symbols.
        /// </summary>
        [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure")]
        public class StringTimeTable {
            private readonly ConcurrentDictionary<string, TimeTracker> _instances = new ConcurrentDictionary<string, TimeTracker>();
            private readonly ConcurrentList<TimeTracker> _trackers = new ConcurrentList<TimeTracker>();
            private readonly Func<string, TimeTracker> _timeFactory;

            public event Action<TimeTracker> InstanceAdded;

            public TimeTracker this[string symbol] => _instances.GetOrAdd(symbol, _timeFactory);

            public StringTimeTable() {
                _timeFactory = new Func<string, TimeTracker>(TimeTrackerFactory);
            }

            private TimeTracker TimeTrackerFactory(string sym) {
                var newTracker = new TimeTracker(sym, false);
                newTracker.SetTime(System.CurrentDateTime);
                _trackers.Add(newTracker);
                InstanceAdded?.Invoke(newTracker);
                return newTracker;
            }

            public bool Contains(string symbol) {
                return _instances.ContainsKey(symbol);
            }

            /// <summary>
            ///     Returns a threadsafe copy of the trackers in this table.
            /// </summary>
            /// <returns></returns>
            public IList<TimeTracker> Trackers() {
                return _trackers;
            }
        }

        /// <summary>
        ///     Provides a <see cref="TimeTracker"/> for the time for 
        /// </summary>
        /// <remarks>During Simulation the tracker keeps the latest time received out of all symbols in <see cref="Instance"/>. During Live it presents <see cref="DateTime.Now"/></remarks>
        public static StringTimeTable Instance { get; } = new StringTimeTable();

        private static readonly ConcurrentList<TimeTracker> _trackers = (ConcurrentList<TimeTracker>) Instance.Trackers();

        /// <summary>
        ///     Provides a <see cref="TimeTracker"/> for the time the system is currently at.
        /// </summary>
        /// <remarks>Tracker keeps the latest time received out of all symbols in <see cref="Instance"/></remarks>
        public static TimeTracker System { get; } = new TimeTracker("/SYSTEM/", true);

        public static DateTime GetTodaysDateTime(DateTime date, TimeSpan time) {
            return date.Date.Add(time);
        }

        /// <summary>
        ///     Sets time specifically at <paramref name="symbol"/> and <see cref="System"/>
        /// </summary>
        /// <param name="symbol">The symbol this time is related to</param>
        /// <param name="dt">The time to set</param>
        public static void SetTime(DateTime dt, TimeTracker tracker) {
            System.SyncTime(dt);
            tracker.SetTime(dt);
        }

        /// <summary>
        ///     Sets time specifically at <paramref name="symbol"/> and <see cref="System"/>
        /// </summary>
        /// <param name="symbol">The symbol this time is related to</param>
        /// <param name="dt">The time to set</param>
        public static void SetTime(string symbol, DateTime dt) {
            System.SyncTime(dt);
            Instance[symbol.ToString()].SetTime(dt);
        }

        /// <summary>
        ///     Sync time at <see cref="System"/> and all registered symbols.
        /// </summary>
        /// <param name="dt">The time to set</param>
        public static void SetTime(DateTime dt) {
            System.SyncTime(dt);
            for (int i = 0; i < _trackers._count; i++) {
                _trackers._arr[i]?.SetTime(dt);
            }
        }

        public static void ScanSchedules(string symbol) {
            System.ScanSchedules();
            Instance[symbol].ScanSchedules();
        }

        public static void ScanSchedules() {
            System.ScanSchedules();
            foreach (var v in Instance.Trackers())
                v.ScanSchedules();
        }

        /// <summary>
        ///     Gets all days between param <paramref name="from"/> up to param <paramref name="to"/>, excluding Saturday and Sunday.
        /// </summary>
        /// <returns>Number of days</returns>
        public static unsafe int TradingDays(long* buffer, Instant from, Instant to) {
            int i = 0;
            while (from <= to) {
                while (from.DayOfWeek == DayOfWeek.Saturday || from.DayOfWeek == DayOfWeek.Sunday)
                    from = from.AddDays(1);

                buffer[i++] = @from.Ticks;
                from = from.AddDays(1);
            }

            return i;
        }

        /// <summary>
        ///     Gets all days between param <paramref name="from"/> up to param <paramref name="to"/>, excluding Saturday and Sunday.
        /// </summary>
        /// <returns>Number of days</returns>
        public static List<Instant> TradingDays(Instant from, Instant to) {
            var l = new List<Instant>((int) Math.Ceiling((to - @from).TotalDays));
            while (from <= to) {
                while (from.DayOfWeek == DayOfWeek.Saturday || from.DayOfWeek == DayOfWeek.Sunday)
                    from = from.AddDays(1);
                l.Add(from);
                from = from.AddDays(1);
            }

            return l;
        }

        /// <summary>
        ///     Gets all days between param <paramref name="from"/> up to param <paramref name="to"/>, excluding Saturday and Sunday.
        /// </summary>
        /// <returns>Number of days</returns>
        public static unsafe int TradingDays(long* buffer, DateTime from, DateTime to) {
            int i = 0;
            while (from <= to) {
                while (from.DayOfWeek == DayOfWeek.Saturday || from.DayOfWeek == DayOfWeek.Sunday)
                    from = from.AddDays(1);

                buffer[i++] = from.Ticks;
                from = from.AddDays(1);
            }

            return i;
        }

        /// <summary>
        ///     Gets all days between param <paramref name="from"/> up to param <paramref name="to"/>, excluding Saturday and Sunday.
        /// </summary>
        public static List<DateTime> TradingDays(DateTime from, DateTime to) {
            var l = new List<DateTime>((int) Math.Ceiling((to - @from).TotalDays));
            while (from <= to) {
                while (from.DayOfWeek == DayOfWeek.Saturday || from.DayOfWeek == DayOfWeek.Sunday)
                    from = from.AddDays(1);
                if (from > to)
                    break;
                l.Add(from);
                from = from.AddDays(1);
            }

            return l;
        }

        /// <summary>
        ///     Gets all days between param <paramref name="from"/> up to param <paramref name="to"/>, excluding Saturday and Sunday in a reveresed order.
        /// </summary>
        public static List<DateTime> ReversedTradingDays(DateTime from, DateTime to) {
            var l = TradingDays(from, to);
            l.Reverse();
            return l;
        }

        public static TimeTrackerQuestioneer Is {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new TimeTrackerQuestioneer(System);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeTrackerQuestioneer IsSymbol(string symbol) =>
            new TimeTrackerQuestioneer(Instance[symbol]);
    }

    public static class TimeExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeTrackerQuestioneer Is(this TimeTracker tracker) =>
            new TimeTrackerQuestioneer(tracker);
    }

    public readonly struct TimeTrackerQuestioneer {
        private readonly TimeTracker _tracker;

        public TimeTrackerQuestioneer(TimeTracker tracker) {
            _tracker = tracker;
        }

        /// <summary>
        ///     Is <paramref name="timeOfDay"/> after current time?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Past(string timeOfDay, bool orEqual = false) {
            return Past(new TimeSpan(Instant.Constants.TicksPerHour * byte.Parse(timeOfDay[0..1]) + Instant.Constants.TicksPerMinute * byte.Parse(timeOfDay[3..4]) + Instant.Constants.TicksPerSecond * byte.Parse(timeOfDay[6..7])), orEqual);
        }

        /// <summary>
        ///     Is <paramref name="timeOfDay"/> after current time?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Past(TimeSpan timeOfDay, bool orEqual = false) {
            if (orEqual)
                return _tracker.CurrentTime >= timeOfDay;
            return _tracker.CurrentTime > timeOfDay;
        }

        /// <summary>
        ///     Is <paramref name="timeOfDay"/> after current time?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Past(DateTime timeOfDay, bool orEqual = false) {
            if (orEqual)
                return _tracker.CurrentDateTime >= timeOfDay;
            return _tracker.CurrentDateTime > timeOfDay;
        }

        /// <summary>
        ///     Is <paramref name="timeOfDay"/> after current time?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Past(long timeOfDay, bool orEqual = false) {
            if (orEqual)
                return (_tracker.TicksNow % 864000000000L) >= timeOfDay;
            return (_tracker.TicksNow % 864000000000L) > timeOfDay;
        }

        /// <summary>
        ///     Is <paramref name="timeOfDay"/> before current time?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Before(TimeSpan timeOfDay, bool orEqual = false) {
            if (orEqual)
                return timeOfDay >= _tracker.CurrentTime;
            return timeOfDay > _tracker.CurrentTime;
        }

        /// <summary>
        ///     Is <paramref name="timeOfDay"/> before current time?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Before(DateTime timeOfDay, bool orEqual = false) {
            if (orEqual)
                return timeOfDay >= _tracker.CurrentDateTime;
            return timeOfDay > _tracker.CurrentDateTime;
        }

        /// <summary>
        ///     Is <paramref name="timeOfDay"/> before current time?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Before(string timeOfDay, bool orEqual = false) {
            return Before(new TimeSpan(Instant.Constants.TicksPerHour * byte.Parse(timeOfDay[0..1]) + Instant.Constants.TicksPerMinute * byte.Parse(timeOfDay[3..4]) + Instant.Constants.TicksPerSecond * byte.Parse(timeOfDay[6..7])), orEqual);
        }

        /// <summary>
        ///     Is <paramref name="timeOfDay"/> before current time?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Before(long timeOfDay, bool orEqual = false) {
            if (orEqual)
                return timeOfDay >= (_tracker.TicksNow % 864000000000L);
            return timeOfDay > (_tracker.TicksNow % 864000000000L);
        }

        /// <summary>
        ///     Is current time <paramref name="preventFrom"/> after and before <paramref name="preventTill"/>  ?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Between(TimeSpan preventFrom, TimeSpan preventTill, bool orEqual = false) {
            return Past(preventFrom, orEqual) && Before(preventTill, orEqual);
        }

        /// <summary>
        ///     Is current time <paramref name="preventFrom"/> after and before <paramref name="preventTill"/>  ?
        /// </summary>
        /// <param name="timeOfDay"></param>
        /// <returns></returns>
        public bool Between(string preventFrom, string preventTill, bool orEqual = false) {
            return Past(new TimeSpan(Instant.Constants.TicksPerHour * byte.Parse(preventFrom[0..1]) + Instant.Constants.TicksPerMinute * byte.Parse(preventFrom[3..4]) + Instant.Constants.TicksPerSecond * byte.Parse(preventFrom[6..7])), orEqual)
                   && Before(new TimeSpan(Instant.Constants.TicksPerHour * byte.Parse(preventTill[0..1]) + Instant.Constants.TicksPerMinute * byte.Parse(preventTill[3..4]) + Instant.Constants.TicksPerSecond * byte.Parse(preventTill[6..7])), orEqual);
        }
    }
}