using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nucs.Timing {
    public struct StopwatchStruct {
        private long elapsed;
        private long startTimeStamp;
        private bool isRunning;

        public static long Frequency => Stopwatch.Frequency;

        /// <summary>Indicates whether the timer is based on a high-resolution performance counter. This field is read-only.</summary>
        public static bool IsHighResolution => Stopwatch.IsHighResolution;

        public static readonly double TickFrequency;

        public DateTime StartTime => new DateTime(IsHighResolution ? (long) (elapsed * TickFrequency) : elapsed);

        static StopwatchStruct() {
            if (!Stopwatch.IsHighResolution) {
                TickFrequency = 1.0;
            } else {
                TickFrequency = 10000000.0;
                TickFrequency /= (double) Frequency;
            }
        }

        public StopwatchStruct(bool start) {
            elapsed = 0;
            isRunning = start;
            startTimeStamp = isRunning ? GetTimestamp() : 0;
        }

        /// <summary>Starts, or resumes, measuring elapsed time for an interval.</summary>
        public void Start() {
            if (isRunning)
                return;
            startTimeStamp = GetTimestamp();
            isRunning = true;
        }

        /// <summary>Prepends the elapsed time to the start point, returning the time passed so far.</summary>
        /// <remarks>Has no isRunning check</remarks>
        public long Checkpoint() {
            var elapsed = GetTimestamp() - startTimeStamp;
            startTimeStamp += elapsed;
            return elapsed * Frequency;
        }

        /// <summary>Initializes a new <see cref="T:System.Diagnostics.StopwatchStruct" /> instance, sets the elapsed time property to zero, and starts measuring elapsed time.</summary>
        /// <returns>A <see cref="T:System.Diagnostics.StopwatchStruct" /> that has just begun measuring elapsed time.</returns>
        public static StopwatchStruct StartNew() {
            return new StopwatchStruct(true);
        }

        /// <summary>Stops measuring elapsed time for an interval.</summary>
        public void Stop() {
            if (!isRunning)
                return;
            elapsed += GetTimestamp() - startTimeStamp;
            isRunning = false;
            if (elapsed >= 0L)
                return;
            elapsed = 0L;
        }


        /// <summary>Stops time interval measurement and resets the elapsed time to zero.</summary>
        public void Reset() {
            this = default;
        }

        public bool YieldIf(long threshold) {
            #if DEBUG
            if (!isRunning) {
                throw new InvalidOperationException("Cant yield when Stopwatch not started.");
            }
            #endif

            var now = GetTimestamp();
            if ((now - startTimeStamp) * TickFrequency >= threshold) {
                startTimeStamp = now;
                elapsed = 0L;
                return true;
            }

            return false;
        }

        /// <summary>Stops time interval measurement, resets the elapsed time to zero, and starts measuring elapsed time.</summary>
        public void Restart() {
            elapsed = 0L;
            startTimeStamp = GetTimestamp();
            isRunning = true;
        }

        /// <summary>Gets a value indicating whether the <see cref="T:System.Diagnostics.StopwatchStruct" /> timer is running.</summary>
        /// <returns>
        /// <see langword="true" /> if the <see cref="T:System.Diagnostics.StopwatchStruct" /> instance is currently running and measuring elapsed time for an interval; otherwise, <see langword="false" />.</returns>

        public bool IsRunning => isRunning;

        /// <summary>Gets the total elapsed time measured by the current instance.</summary>
        /// <returns>A read-only <see cref="T:System.TimeSpan" /> representing the total elapsed time measured by the current instance.</returns>

        public TimeSpan Elapsed => new TimeSpan(GetElapsedDateTimeTicks());

        /// <summary>Gets the total elapsed time measured by the current instance, in milliseconds.</summary>
        /// <returns>A read-only long integer representing the total number of milliseconds measured by the current instance.</returns>

        public long ElapsedMilliseconds => GetElapsedDateTimeTicks() / 10000L;

        /// <summary>Gets the total elapsed time measured by the current instance, in timer ticks.</summary>
        /// <returns>A read-only long integer representing the total number of timer ticks measured by the current instance.</returns>

        public long ElapsedTicks => GetRawElapsedTicks();


        /// <summary>Gets the current number of ticks in the timer mechanism.</summary>
        /// <returns>A long integer representing the tick counter value of the underlying timer mechanism.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetTimestamp() {
            if (!Stopwatch.IsHighResolution)
                return DateTime.UtcNow.Ticks;
            Native.QueryPerformanceCounter(out var num);
            return num;
        }

        private long GetRawElapsedTicks() {
            long elapsed = this.elapsed;
            if (!isRunning)
                return elapsed;
            long num = GetTimestamp() - startTimeStamp;
            elapsed += num;

            return elapsed;
        }

        private long GetElapsedDateTimeTicks() {
            long rawElapsedTicks = GetRawElapsedTicks();
            return Stopwatch.IsHighResolution ? (long) ((double) rawElapsedTicks * TickFrequency) : rawElapsedTicks;
        }

        private static class Native {
            [DllImport("kernel32.dll")]
            public static extern bool QueryPerformanceCounter(out long value);


            [DllImport("kernel32.dll")]
            private static extern bool QueryPerformanceFrequency(out long value);
        }
    }
}