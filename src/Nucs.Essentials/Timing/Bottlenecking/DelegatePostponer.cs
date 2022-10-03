using System;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Extensions;

namespace Nucs.Timing.Bottlenecking {
    /// <summary>
    ///     Assigns a low priority task to perform a certain method every <see cref="RefreshRate"/>
    ///     where <see cref="NextUpdate"/> - when not 0, will be triggering that delegate. that delay can be postponed again and again to prevent execution too soon.
    /// </summary>
    public class DelegatePostponer : IDisposable {
        public long NextUpdate;
        public readonly long RefreshRate;
        public readonly Action UpdateDelegate;
        private readonly SemaphoreSlim idling = new SemaphoreSlim(0, 1);
        private CancellationTokenSource source;

        public DelegatePostponer(TimeSpan refreshRate, Action updateDelegate, bool start = false) : this(refreshRate.Ticks, updateDelegate, start) { }

        public DelegatePostponer(long refreshRate, Action updateDelegate, bool start = false) {
            RefreshRate = refreshRate;
            UpdateDelegate = updateDelegate;
            source = null;

            if (start)
                StartTask();
        }

        public void Postpone() {
            PostponeIn(RefreshRate);
        }

        public void PostponeIn(TimeSpan duration) {
            PostponeIn(duration.Ticks);
        }

        public void PostponeIn(long duration) {
            Interlocked.Exchange(ref NextUpdate, DateTime.UtcNow.Ticks + duration);

            if (idling.CurrentCount == 0)
                idling.ReleaseTo(1);
        }

        public void UpdateASAP() {
            PostponeIn(-1);
        }

        public void AbortUpdate() {
            Interlocked.Exchange(ref NextUpdate, 0);
        }

        public void StartTask() {
            if (source != null)
                return;

            source = new CancellationTokenSource();
            StartInternalTask();
        }

        public void StopTask() {
            Interlocked.Exchange(ref NextUpdate, 0);
            source.SafeCancel();
            source = null;
        }

        private void StartInternalTask() {
            CancellationToken token = source.Token;
            _ = Task.Run(async () => {
                try {
                    while (!token.IsCancellationRequested) {
                        if (Interlocked.Read(ref NextUpdate) == 0) {
                            await idling.WaitAsync(token).ConfigureAwait(false);
                            if (token.IsCancellationRequested)
                                break;
                        }

                        _retry:
                        var nextUpdate = Interlocked.Read(ref NextUpdate);
                        long now = DateTime.UtcNow.Ticks;
                        if (now < nextUpdate) {
                            await Task.Delay(new TimeSpan(nextUpdate - now), token).ConfigureAwait(false);
                            goto _retry;
                        }

                        if (nextUpdate == Interlocked.CompareExchange(ref NextUpdate, 0, nextUpdate) && !token.IsCancellationRequested) {
                            UpdateDelegate();
                        }
                    }
                } catch (OperationCanceledException) {
                    //swallow
                }
            }, source.Token);
        }

        public void Dispose() {
            source?.Dispose();
        }
    }
}