using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Nucs.Exceptions;
using Nucs.Extensions;

namespace Nucs.Timing.Scheduling {
    [SuppressMessage("ReSharper", "HeapView.DelegateAllocation")]
    public class EventScheduler : IDisposable {
        private readonly ScheduleMap _scheduleMap;

        private readonly TimeTracker _tracker;
        private DateTime nextInvoke;

        /// <summary>
        ///     When will be the closest event invoked?
        /// </summary>
        public DateTime NextEvent => nextInvoke;

        public EventScheduler(TimeTracker tracker, bool threadSafe) {
            _tracker = tracker;
            _scheduleMap = new ScheduleMap(tracker, threadSafe);
        }

        public sealed class ScheduleMap : C5.IntervalHeap<KeyValuePair<DateTime, ScheduleDelegate>> {
            private readonly TimeTracker _tracker;
            public readonly bool Threadsafe;
            #if DEBUG
            public DateTime NextInvoke;
            #endif

            public long ScheduledItems;

            public ScheduleMap(TimeTracker tracker, bool threadsafe) : base(new KeyValuePairComparer<ScheduleDelegate>()) {
                _tracker = tracker;
                Threadsafe = threadsafe;
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            public void Scan() {
                // ReSharper disable TooWideLocalVariableScope
                bool isEmpty = IsEmpty;
                KeyValuePair<DateTime, ScheduleDelegate> target;
                while (!isEmpty) {
                    try {
                        //we only lock the dequeue part
                        target = FindMin(); //dequeues the target with smallest wait time

                        if (target.Key.Ticks - _tracker.TicksNow > 0)
                            break;

                        target = DeleteMin();
                        isEmpty = IsEmpty;
                        #if DEBUG
                        NextInvoke = isEmpty ? DateTime.MinValue : FindMin().Key;
                        #endif
                    } catch (ItemNotFoundException e) {
                        break;
                    }

                    try {
                        target.Value();
                    } catch (Exception e) {
                        SystemHelper.Logger?.Error($"A scheduled job at {target.Key} has failed ({target.Value?.ToString()}).\n" + e);
                        ApplicationEvents.OnException(e, false);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            public void ScanThreadsafe() {
                // ReSharper disable TooWideLocalVariableScope
                bool isEmpty = IsEmpty;
                KeyValuePair<DateTime, ScheduleDelegate> target;
                while (!isEmpty) {
                    //we only lock the dequeue part
                    Monitor.Enter(this);
                    try {
                        if (IsEmpty) //was it updated within the lock
                            break;

                        target = FindMin(); //dequeues the target with smallest wait time

                        if (target.Key.Ticks - _tracker.TicksNow > 0)
                            break;

                        target = DeleteMin();
                        isEmpty = IsEmpty;
                        #if DEBUG
                        NextInvoke = isEmpty ? DateTime.MinValue : FindMin().Key;
                        #endif
                    } catch (ItemNotFoundException e) {
                        break;
                    } finally {
                        Monitor.Exit(this);
                    }

                    try {
                        target.Value();
                    } catch (Exception e) {
                        SystemHelper.Logger?.Error($"A scheduled job at {target.Key} has failed ({target.Value?.ToString()}).\n" + e);
                        ApplicationEvents.OnException(e, false);
                    }
                }
            }
        }

        private int _ticksOffset;

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField"), MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void ScanSchedules() {
            if (_scheduleMap.IsEmpty) return;
            if (_scheduleMap.Threadsafe)
                _scheduleMap.ScanThreadsafe();
            else

                _scheduleMap.Scan();
            return;
        }

        private void _internalSchedule(KeyValuePair<DateTime, ScheduleDelegate> item) {
            if (item.Key <= _tracker.CurrentDateTime) {
                if (item.Key.Date < _tracker.CurrentDate)
                    SystemHelper.Logger?.Error($"Task is scheduled to be executed yesterday, Investigate Stacktrace: \n{Environment.StackTrace}");

                try {
                    item.Value.Invoke();
                } catch (Exception e) {
                    SystemHelper.Logger?.Error($"A scheduled job at {item.Key} has failed ({item.Value?.ToString()}).\n" + e);
                    ApplicationEvents.OnException(e, false);
                }
            } else
                lock (_scheduleMap)
                    _scheduleMap.Add(item);
        }

        public void ScheduleWithin(TimeSpan time, object context, ScheduleContextedDelegate del) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(_tracker.CurrentDateTime.Add(time), () => del(context)));
        }

        public void ScheduleWithin(TimeSpan time, ScheduleDelegate del) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(_tracker.CurrentDateTime.Add(time), del));
        }

        public void ScheduleNext(object context, ScheduleContextedDelegate del) {
            var item = new KeyValuePair<DateTime, ScheduleDelegate>(_tracker.CurrentDateTime.AddTicks(++_ticksOffset), () => del(context));
            lock (_scheduleMap) _scheduleMap.Add(item);
        }

        public void ScheduleNext(ScheduleDelegate del) {
            var item = new KeyValuePair<DateTime, ScheduleDelegate>(_tracker.CurrentDateTime.AddTicks(++_ticksOffset), del);
            lock (_scheduleMap) _scheduleMap.Add(item);
        }

        public void ScheduleTodayAt(ScheduleContextedDelegate del, TimeSpan time, object context) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(_tracker.CurrentDate.Add(time), () => del(context)));
        }

        public void ScheduleTodayAt(TimeSpan time, ScheduleDelegate del) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(_tracker.CurrentDate.Add(time), del));
        }

        public void ScheduleTomorrowAt(TimeSpan time, object context, ScheduleContextedDelegate del) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(_tracker.CurrentDate.AddDays(1).Add(time), () => del(context)));
        }

        public void ScheduleTomorrowAt(TimeSpan time, ScheduleDelegate del) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(_tracker.CurrentDate.AddDays(1).Add(time), del));
        }

        public void ScheduleAt(DateTime time, object context, ScheduleContextedDelegate del) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(time, () => del(context)));
        }

        public void ScheduleAt(DateTime time, ScheduleDelegate del) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(time, del));
        }

        public IntervalRegistry ScheduleInterval(DateTime startingFrom, TimeSpan interval, object context, ScheduledIntervalContextedDelegate del, ulong? maxTriggers = null) {
            return ScheduleInterval(startingFrom, interval, reg => del(reg, context), maxTriggers);
        }

        public IntervalRegistry ScheduleInterval(TimeSpan interval, object context, ScheduledIntervalContextedDelegate del, ulong? maxTriggers = null) {
            return ScheduleInterval(interval, reg => del(reg, context), maxTriggers);
        }

        public IntervalRegistry ScheduleInterval(TimeSpan interval, ScheduledIntervalDelegate del, ulong? maxTriggers = null) {
            return new IntervalRegistry(this, del, interval, maxTriggers);
        }

        public IntervalRegistry ScheduleInterval(DateTime startingFrom, TimeSpan interval, ScheduledIntervalDelegate del, ulong? maxTriggers = null) {
            return new IntervalRegistry(this, del, startingFrom, interval, maxTriggers);
        }

        public class IntervalRegistry {
            private readonly CancellationTokenSource _cancelSource;
            private readonly ScheduledIntervalDelegate _del;
            private readonly EventScheduler _scheduler;
            private readonly long _startingFrom;

            /// <summary>
            ///     Property that can be used by developer between the interval calls.
            /// </summary>
            public object Tag;

            public DateTime StartingFrom => new DateTime(_startingFrom);
            public DateTime NextSchedule => new DateTime(_nextSchedule);

            public ulong Triggers => _triggers;

            private long _intervalTicks;
            private long _nextSchedule;
            private ulong _triggers;
            private ulong? _maxTiggers;

            public IntervalRegistry(EventScheduler scheduler, ScheduledIntervalDelegate del, DateTime startingFrom, TimeSpan interval, ulong? numberOfTriggers = null) {
                _del = del;
                _maxTiggers = numberOfTriggers;
                _scheduler = scheduler;
                Interval = interval;
                _cancelSource = new CancellationTokenSource();
                //we assign schedule for startingFrom
                _startingFrom = startingFrom.Ticks;
                _nextSchedule = _startingFrom;

                ScheduleAt(StartingFrom);
            }

            public IntervalRegistry(EventScheduler scheduler, ScheduledIntervalDelegate del, TimeSpan interval, ulong? numberOfTriggers = null) {
                Interval = interval;
                _del = del;
                _maxTiggers = numberOfTriggers;
                _scheduler = scheduler;
                _cancelSource = new CancellationTokenSource();

                //we assign schedule starting from interval
                _startingFrom = scheduler._tracker.TicksNow + _intervalTicks;
                _nextSchedule = _startingFrom;

                ScheduleAt(StartingFrom);
            }

            private void _delegateWrapper() {
                if (IsCancelled)
                    return; //task was cancelled

                _triggers++;
                if (SkipTriggers > 0) {
                    SkipTriggers--;
                    if (!IsCancelled) {
                        _nextSchedule += _intervalTicks;
                        ScheduleAt(new DateTime(_nextSchedule));
                    }

                    return; //task is skipped
                }

                if (_maxTiggers.HasValue) {
                    if (_maxTiggers.Value <= 0)
                        return; //no triggers left

                    _maxTiggers--;
                }

                try {
                    _del(this);
                } catch (Exception e) {
                    SystemHelper.Logger?.ErrorException($"An interval scheduled job has failed.", e);
                    ApplicationEvents.OnException(e, false);
                }

                //reschedule
                if (!IsCancelled) {
                    _nextSchedule += _intervalTicks;
                    ScheduleAt(new DateTime(_nextSchedule));
                }
            }

            private void ScheduleAt(DateTime at) {
                _scheduler.ScheduleAt(at, _delegateWrapper);
            }

            public bool IsCancelled => _cancelSource.IsCancellationRequested;

            /// <summary>
            /// How many triggers should we skip
            /// </summary>
            public ulong SkipTriggers { get; set; }

            /// <summary>
            ///     The interval atwhich this schedule triggers.
            /// </summary>
            public TimeSpan Interval {
                get => new TimeSpan(_intervalTicks);
                set => _intervalTicks = value.Ticks;
            }

            public void Cancel() {
                _cancelSource.SafeCancel();
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose() {
            //nop
        }
    }
}