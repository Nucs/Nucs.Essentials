using System;
using System.Runtime.CompilerServices;
using Nucs.Timing.Scheduling;

namespace Nucs.Timing {
    public sealed class TimeTracker {
        private readonly EventScheduler _scheduler;
        private SimulatedTimeProvider _time;

        public string Symbol { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTime(DateTime time) {
            _time.Sync(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncTime(DateTime time) {
            if (_time.TicksNow <= time.Ticks || time.Date != _time.Today) {
                SetTime(time);
            }
        }

        public void SetTimeIfEmpty(DateTime time) {
            if (_time.Now == DateTime.MinValue) {
                SetTime(time);
            }
        }

        public DateTime CurrentDateTime => _time.Now;

        public DateTime CurrentDate => _time.Today;

        public TimeSpan CurrentTime => new TimeSpan(_time.TicksNow % 864000000000L);

        public long TicksNow => _time.TicksNow;

        /// <summary>
        ///     Helper class to easily identify current time.
        /// </summary>
        public readonly TimeTrackerQuestioneer Is;

        public TimeTracker(string symbol, bool threadSafe) {
            Symbol = symbol;
            _time = new SimulatedTimeProvider();
            _scheduler = new EventScheduler(this, threadSafe);
            Is = new TimeTrackerQuestioneer(this);
        }

        #region Scheduling

        public void ScanSchedules() {
            _scheduler.ScanSchedules();
        }

        public void ScheduleAt(DateTime time, object context, ScheduleContextedDelegate del) {
            _scheduler.ScheduleAt(time, context, del);
        }

        public void ScheduleAt(DateTime time, ScheduleDelegate del) {
            _scheduler.ScheduleAt(time, del);
        }

        public void ScheduleTodayAt(TimeSpan time, object context, ScheduleContextedDelegate del) {
            _scheduler.ScheduleTodayAt(del, time, context);
        }

        public void ScheduleTodayAt(TimeSpan time, ScheduleDelegate del) {
            _scheduler.ScheduleTodayAt(time, del);
        }

        public void ScheduleTomorrowAt(TimeSpan time, object context, ScheduleContextedDelegate del) {
            _scheduler.ScheduleTomorrowAt(time, context, del);
        }

        public void ScheduleTomorrowAt(TimeSpan time, ScheduleDelegate del) {
            _scheduler.ScheduleTomorrowAt(time, del);
        }

        public void ScheduleWithin(TimeSpan time, object context, ScheduleContextedDelegate del) {
            _scheduler.ScheduleWithin(time, context, del);
        }

        public void ScheduleWithin(TimeSpan time, ScheduleDelegate del) {
            _scheduler.ScheduleWithin(time, del);
        }

        public void ScheduleNext(object context, ScheduleContextedDelegate del) {
            _scheduler.ScheduleNext(context, del);
        }

        public void ScheduleNext(ScheduleDelegate del) {
            _scheduler.ScheduleNext(del);
        }

        public EventScheduler.IntervalRegistry ScheduleInterval(DateTime startingFrom, TimeSpan interval, object context, ScheduledIntervalContextedDelegate del, ulong? maxTriggers = null) {
            return _scheduler.ScheduleInterval(startingFrom, interval, context, del, maxTriggers);
        }

        public EventScheduler.IntervalRegistry ScheduleInterval(TimeSpan interval, object context, ScheduledIntervalContextedDelegate del, ulong? maxTriggers = null) {
            return _scheduler.ScheduleInterval(interval, context, del, maxTriggers);
        }

        public EventScheduler.IntervalRegistry ScheduleInterval(TimeSpan interval, ScheduledIntervalDelegate del, ulong? maxTriggers = null) {
            return _scheduler.ScheduleInterval(interval, del, maxTriggers);
        }

        public EventScheduler.IntervalRegistry ScheduleInterval(DateTime startingFrom, TimeSpan interval, ScheduledIntervalDelegate del, ulong? maxTriggers = null) {
            return _scheduler.ScheduleInterval(startingFrom, interval, del, maxTriggers);
        }

        #endregion
    }
}