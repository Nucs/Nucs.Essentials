using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Nucs.Collections;
using Nucs.Collections.Structs;

namespace Nucs.Timing {
    public struct SimpleMovingStats {
        private RollingWindowStruct<int> _window; //(long - long) can be stored as int

        private long _cumulativePrice;

        public long Average; //unwarpped TimeSpan
        public long Peak; //unwarpped TimeSpan
        public int PeakedAtIndex;

        public int Period { get; }

        public long Samples => _window.Samples;

        public bool IsReady => _window.IsReady;

        public SimpleMovingStats(int period) {
            if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period));
            Period = period;
            _window = new RollingWindowStruct<int>(period);
            _cumulativePrice = 0;
            Average = 0;
            Peak = 0;
            PeakedAtIndex = 0;
        }

        public void Update(int value) {
            _cumulativePrice -= _window.Push(value);
            _cumulativePrice += value;
            Average = _cumulativePrice / _window.Count;

            if (value > Peak) {
                Peak = value;
                PeakedAtIndex = _window.Count - 1; //index of latest pushed 
            } else if (--PeakedAtIndex <= -1) {
                //pop the peak value and find the actual new peak
                Peak = default;
                //resolve the newest peak level
                var (arr, count) = _window.Data;
                for (int i = 0; i < count; i++) {
                    if (arr[i] > Peak) {
                        Peak = arr[i];
                        PeakedAtIndex = i;
                    }
                }
            }
        }

        public void Reset() {
            _window.Reset();
            _cumulativePrice = 0;
            Average = default;
            Peak = default;
        }

        //public static void Test() {
        //    var sma = new SimpleMovingAverageIndicator(3);
        //    sma.Update(5).Should().Be(5d);
        //    sma.Update(10).Should().Be(7.5d);
        //    sma.Update(15).Should().Be(10d);
        //    sma.Current.Should().Be(10);
        //    sma.Update(20).Should().Be(15);
        //    sma.Reset();
        //    sma.Current.Should().Be(0);
        //    sma.Update(-5).Should().Be(-5d);
        //}
    }

    public struct RollingLatency {
        public bool Changed;
        public long Samples => Stats.Samples;

        public TimeSpan AverageLatency => new TimeSpan(Stats.Average);
        public TimeSpan PeakLatency => new TimeSpan(Stats.Peak);

        public bool HasValue => Samples > 0;

        private SimpleMovingStats Stats;

        public RollingLatency(int size) : this() {
            Stats = new SimpleMovingStats(size);
        }

        public void Sample(long incomingStamp, long now) {
            unchecked {
                Stats.Update((int) (now - incomingStamp));
                Changed = true;
            }
        }
    }

    public class LatencyCollector {
        public static readonly Pool CollectorsPool = new Pool();
        private static StringBuilder s_loggerBuilder;

        public readonly string Source;
        public string ReportMessage { get; }
        public RollingLatency Sampler;

        public void Sample(long incomingStamp, long now) {
            #if DEBUG
            //do not sample .Date stamps
            if (now - incomingStamp == Instant.ToTimeOfDay(now))
                throw new NotSupportedException($"Unable to sample TimeOfDay, please investigate callstack and prevent it from happening");
            #endif
            Sampler.Sample(incomingStamp, now);
        }

        public LatencyCollector(string source, string reportMessage, int samplingWindow) {
            Source = source;
            ReportMessage = reportMessage;
            Sampler = new RollingLatency(samplingWindow);
            CollectorsPool.Add(this); //map this
        }

        public override string ToString() {
            return string.Format($"{Source}: {ReportMessage}", Sampler.AverageLatency.ToString(AbstractInfrastructure.TimeLongFormat), Sampler.Samples.ToString(), Sampler.PeakLatency.ToString(AbstractInfrastructure.TimeLongFormat));
        }

        public class Pool {
            private bool _timerEnabled;
            private Timer _timer;
            public readonly ConcurrentList<LatencyCollector> Collectors = new ConcurrentList<LatencyCollector>(16);
            public Action<string> _logFunction;

            public void Assign(Action<string> logFunction) {
                _logFunction = logFunction;
            }

            public Pool() { }

            public bool TimerEnabled {
                get => _timerEnabled;
                private set {
                    if (_timerEnabled == value)
                        return;

                    _timer?.Dispose();
                    _timer = null;
                    if (value) {
                        _timer = new Timer(TimerCallback, this, 0, 30000);
                    }

                    _timerEnabled = value;
                }
            }

            public static void TimerCallback(object state) {
                s_loggerBuilder ??= new StringBuilder(1024);
                var sb = s_loggerBuilder;
                sb.Clear();
                sb.Append('\n');

                var pool = (Pool) state;
                var len = pool.Collectors.Count;
                var arr = pool.Collectors.InternalArray;
                string msg;
                for (int i = 0; i < len; i++) {
                    ref var a = ref arr[i];
                    if (!a.Sampler.Changed || !a.Sampler.HasValue)
                        continue;
                    a.Sampler.Changed = false;
                    msg = $"[{i + 1}] {a}";
                    if (i != len - 1)
                        sb.AppendLine(msg);
                    else sb.Append(msg);
                }

                msg = sb.ToString().Trim('\n', '\r', ' ', '\t');
                if (string.IsNullOrEmpty(msg))
                    return;

                pool._logFunction(msg);
            }

            public void Map(LatencyCollector coll) {
                if (!TimerEnabled) {
                    TimerEnabled = true;
                }
            }

            public void Add(LatencyCollector coll) {
                Collectors.Add(coll);
                if (Collectors.Count > 0)
                    TimerEnabled = true;
            }

            public void Remove(LatencyCollector coll) {
                if (Collectors.Remove(coll)) {
                    if (Collectors.Count == 0)
                        TimerEnabled = false;
                }
            }
        }
    }
}