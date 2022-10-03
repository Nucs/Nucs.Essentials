using System;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Extensions;

namespace Nucs.Timing.Bottlenecking {
    /// <summary>
    ///     Assigns a low priority task to perform a certain method every <see cref="RefreshRate"/>
    ///     where <see cref="NextUpdate"/> - when not 0, will be triggering that delegate. that delay can be postponed again and again to prevent execution too soon.
    /// </summary>
    public class RatedEvent : IDisposable {
        public long NextUpdate;
        public readonly long RefreshRate;
        public event Action UpdateDelegate;
        private CancellationTokenSource source;

        public RatedEvent(TimeSpan refreshRate, Action updateDelegate = null, bool start = false) : this(refreshRate.Ticks, updateDelegate, start) { }

        public RatedEvent(long refreshRate, Action updateDelegate = null, bool start = false) {
            RefreshRate = refreshRate;
            UpdateDelegate = updateDelegate;
            source = null;

            if (start)
                Start();
        }

        public void AddUpdateDelegate(Action del) =>
            UpdateDelegate += del;

        public void PostponeUpdate() {
            Interlocked.Exchange(ref NextUpdate, DateTime.UtcNow.Ticks + RefreshRate);
        }

        public void AbortUpdate() {
            Interlocked.Exchange(ref NextUpdate, 0);
        }

        public void UpdateNextRefresh() {
            Interlocked.Exchange(ref NextUpdate, DateTime.UtcNow.Ticks);
        }

        public void Start() {
            if (source != null)
                return;

            source = new CancellationTokenSource();
            Task.Run(CoreTask, source.Token);
        }

        public void Stop() {
            Interlocked.Exchange(ref NextUpdate, 0);
            source.SafeCancel();
            source = null;
        }

        private async Task CoreTask() {
            CancellationToken token = source.Token;
            while (!token.IsCancellationRequested) {
                if (Interlocked.Read(ref NextUpdate) == 0) {
                    await Task.Delay(new TimeSpan(RefreshRate), token).ConfigureAwait(false);
                    continue;
                }

                var nextUpdate = Interlocked.Read(ref NextUpdate);
                long now = DateTime.UtcNow.Ticks;
                if (now < nextUpdate)
                    await Task.Delay(new TimeSpan(nextUpdate - now), token).ConfigureAwait(false);

                UpdateDelegate();
            }
        }

        public void Dispose() {
            source?.Dispose();
        }
    }
}