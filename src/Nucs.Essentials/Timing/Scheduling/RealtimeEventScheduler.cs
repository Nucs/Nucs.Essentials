//this is not used currently.


/*using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SystemInfrastructure;
using SystemInfrastructure.Data;

namespace SystemInfrastructureLib {
    public class EventScheduler : IEventScheduler {
        private readonly C5.IntervalHeap<KeyValuePair<DateTime, ScheduleDelegate>> _scheduledItems
            = new C5.IntervalHeap<KeyValuePair<DateTime, ScheduleDelegate>>(new KeyValuePairComparer<ScheduleDelegate>());

        private ITimeProvider _time;
        private SemaphoreSlim _scheduledItemSignal = new SemaphoreSlim(0, int.MaxValue);
        private CancellationTokenSource _cancel = new CancellationTokenSource();
        private CancellationTokenSource _cancelWaiting = new CancellationTokenSource();
        private DateTime nextInvoke => target.Key;
        private KeyValuePair<DateTime, ScheduleDelegate> target;

        /// <summary>
        ///     When will be the closest event invoked?
        /// </summary>
        public DateTime NextEvent => nextInvoke;

        public EventScheduler(ITimeProvider time) {
            _time = time;
            Task.Run(async () => await Scheduler().ConfigureAwait(false));
        }

        public async Task Scheduler() {
            var token = _cancel.Token;

            while (!token.IsCancellationRequested) {
                await _scheduledItemSignal.WaitAsync(token).ConfigureAwait(false);

                lock (_scheduledItems) //we only lock the dequeue part
                {
                    if (_scheduledItems.IsEmpty)
                        continue;

                    target = _scheduledItems.DeleteMin(); //dequeues the target with smallest wait time
                    if (_cancelWaiting.IsCancellationRequested)
                        _cancelWaiting = new CancellationTokenSource();
                }

                try {
                    using var shared = CancellationTokenSource.CreateLinkedTokenSource(token, _cancelWaiting.Token);
                    var sleepTime = (int) (target.Key - _time.Now).TotalMilliseconds;
                    if (10 < sleepTime) //ignore delay of less than 10ms.
                        await Task.Delay(sleepTime, shared.Token).ConfigureAwait(false);
                } catch (TaskCanceledException) {
                    continue;
                } catch (ObjectDisposedException) {
                    continue;
                }

                try {
                    target.Value();
                } catch (Exception e) {
                    ApplicationEvents.OnException(e, false);
                }
            }
        }

        private void _internalSchedule(KeyValuePair<DateTime, ScheduleDelegate> item) {
            lock (_scheduledItems) {
                if (_scheduledItems.Count > 0 && (nextInvoke - _time.Now).TotalMilliseconds > 20 && (nextInvoke - item.Key).TotalMilliseconds > 10) //if currently waited is later than newly scheduled and newly 
                {
                    if (!_cancelWaiting.IsCancellationRequested)
                        _cancelWaiting.Cancel();

                    _scheduledItems.Add(item);
                    _scheduledItems.Add(target);
                    _scheduledItemSignal.Release(2);
                } else {
                    _scheduledItems.Add(item);
                    _scheduledItemSignal.Release(1);
                }
            }
        }

        /// <summary>
        ///     Syncronizes this object based on given <paramref name="data"/>.
        /// </summary>
        /// <param name="data"></param>
        public void Sync(ITimedObject data) {
            //nop
        }

        /// <summary>
        ///     Syncronizes this object based on given <paramref name="data"/>.
        /// </summary>
        /// <param name="data"></param>
        public void Sync(DateTime data) {
            //nop
        }

        public void ScheduleAt(ScheduleDelegate del, TimeSpan time) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(_time.Today.Add(time), del));
        }

        public void ScheduleTodayAt(ScheduleContextedDelegate del, TimeSpan time, object obj) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(_time.Today.Add(time), () => del(obj)));
        }

        public void ScheduleAt(ScheduleContextedDelegate del, DateTime time, object obj) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(time, () => del(obj)));
        }

        public void ScheduleAt(ScheduleDelegate del, DateTime time) {
            _internalSchedule(new KeyValuePair<DateTime, ScheduleDelegate>(time, del));
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose() {
            _scheduledItemSignal?.Dispose();
            _cancel?.Dispose();
            _cancelWaiting?.Dispose();
        }
    }
}*/

