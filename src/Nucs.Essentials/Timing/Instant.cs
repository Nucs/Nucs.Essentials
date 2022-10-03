using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Nucs.Timing {
    [Serializable]
    [StructLayout(LayoutKind.Auto)]
    [ComVisible(true)]

    public readonly partial struct Instant : IComparable<Instant>, IComparable, IEquatable<Instant> {
        public readonly long Ticks;
        
        public Instant(long ticks) {
            Ticks = ticks;
        }

        public Instant(ulong ticks) : this((long) ticks) { }

        /// <summary>Gets the day of the month represented by this instance.</summary>
        /// <returns>The day component, expressed as a value between 1 and 31.</returns>
        [JsonIgnore]
        public int Day => GetDatePart(3);

        /// <summary>Gets the day of the week represented by this instance.</summary>
        /// <returns>An enumerated constant that indicates the day of the week of this <see cref="T:System.DateTime" /> value.</returns>
        [JsonIgnore]
        public DayOfWeek DayOfWeek => (DayOfWeek) ((Ticks / 864000000000L + 1L) % 7L);

        /// <summary>Gets the day of the year represented by this instance.</summary>
        /// <returns>The day of the year, expressed as a value between 1 and 366.</returns>
        [JsonIgnore]
        public int DayOfYear => GetDatePart(1);

        /// <summary>Gets the hour component of the date represented by this instance.</summary>
        /// <returns>The hour component, expressed as a value between 0 and 23.</returns>
        [JsonIgnore]
        public int Hour => (int) (Ticks / 36000000000L % 24L);

        /// <summary>Gets the milliseconds component of the date represented by this instance.</summary>
        /// <returns>The milliseconds component, expressed as a value between 0 and 999.</returns>
        [JsonIgnore]
        public int Millisecond => (int) (Ticks / 10000L % 1000L);

        /// <summary>Gets the minute component of the date represented by this instance.</summary>
        /// <returns>The minute component, expressed as a value between 0 and 59.</returns>
        [JsonIgnore]
        public int Minute => (int) (Ticks / 600000000L % 60L);

        /// <summary>Gets the month component of the date represented by this instance.</summary>
        /// <returns>The month component, expressed as a value between 1 and 12.</returns>
        [JsonIgnore]
        public int Month => GetDatePart(2);

        // Returns the year part of this DateTime. The returned value is an
        // integer between 1 and 9999.
        [JsonIgnore]
        public int Year => GetDatePart(0);

        /// <summary>Gets the value of the current <see cref="T:System.TimeSpan" /> structure expressed in whole and fractional days.</summary>
        /// <returns>The total number of days represented by this instance.</returns>
        [JsonIgnore]
        public double TotalDays => Ticks * 1.15740740740741E-12;

        /// <summary>Gets the value of the current <see cref="T:System.TimeSpan" /> structure expressed in whole and fractional hours.</summary>
        /// <returns>The total number of hours represented by this instance.</returns>
        [JsonIgnore]
        public double TotalHours => Ticks * 2.77777777777778E-11;

        /// <summary>Gets the value of the current <see cref="T:System.TimeSpan" /> structure expressed in whole and fractional milliseconds.</summary>
        /// <returns>The total number of milliseconds represented by this instance.</returns>
        [JsonIgnore]
        public double TotalMilliseconds {
            get {
                double num = Ticks * 0.0001;
                if (num > 922337203685477.0)
                    return 922337203685477.0;
                return num < -922337203685477.0 ? -922337203685477.0 : num;
            }
        }

        /// <summary>Gets the value of the current <see cref="T:System.TimeSpan" /> structure expressed in whole and fractional minutes.</summary>
        /// <returns>The total number of minutes represented by this instance.</returns>

        [JsonIgnore]
        public double TotalMinutes => Ticks * 1.66666666666667E-09;

        /// <summary>Gets the value of the current <see cref="T:System.TimeSpan" /> structure expressed in whole and fractional seconds.</summary>
        /// <returns>The total number of seconds represented by this instance.</returns>

        [JsonIgnore]
        public double TotalSeconds => Ticks * 1E-07;

        [JsonIgnore]
        public Instant Date {
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            get => new Instant(unchecked(Ticks - Ticks % 864000000000L));
        }

        [JsonIgnore]
        public long DateTicks {
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            get => unchecked(Ticks - Ticks % 864000000000L);
        }

        public static Instant NowUtc => DateTime.UtcNow.Ticks;

        public static Instant Now => DateTime.Now.Ticks;

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() =>
            (int) Ticks ^ (int) (Ticks >> 32);

        private static readonly long DateTimeThreshold = new DateTime(1900, 1, 1).Ticks;

        public override string ToString() {
            return Ticks > DateTimeThreshold ? new DateTime(Ticks).ToString("s") : new TimeSpan(Ticks).ToString();
        }

        #region Relational members

        public bool AreSameDate(in Instant other) {
            return unchecked(Ticks - Ticks % 864000000000L) == unchecked(other.Ticks - other.Ticks % 864000000000L);
        }

        public int CompareTo(Instant other) {
            if (Ticks < other.Ticks)
                return -1;
            return Ticks > other.Ticks ? 1 : 0;
        }

        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is Instant other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Instant)}");
        }

        #endregion

        #region Private

        private static long DateToTicks(int year, int month, int day) {
            if (year >= 1 && year <= 9999 && (month >= 1 && month <= 12)) {
                int[] numArray = DateTime.IsLeapYear(year) ? Constants.DaysToMonth366 : Constants.DaysToMonth365;
                if (day >= 1 && day <= numArray[month] - numArray[month - 1]) {
                    int num = year - 1;
                    return (num * 365 + num / 4 - num / 100 + num / 400 + numArray[month - 1] + day - 1) * 864000000000L;
                }
            }

            throw new ArgumentOutOfRangeException(null, "ArgumentOutOfRange_BadYearMonthDay");
        }


        private int GetDatePart(int part) {
            int num1 = (int) (Ticks / 864000000000L);
            int num2 = num1 / 146097;
            int num3 = num1 - num2 * 146097;
            int num4 = num3 / 36524;
            if (num4 == 4)
                num4 = 3;
            int num5 = num3 - num4 * 36524;
            int num6 = num5 / 1461;
            int num7 = num5 - num6 * 1461;
            int num8 = num7 / 365;
            if (num8 == 4)
                num8 = 3;
            if (part == 0)
                return num2 * 400 + num4 * 100 + num6 * 4 + num8 + 1;
            int num9 = num7 - num8 * 365;
            if (part == 1)
                return num9 + 1;
            int[] numArray = num8 == 3 && (num6 != 24 || num4 == 3) ? Constants.DaysToMonth366 : Constants.DaysToMonth365;
            int index = num9 >> 6;
            while (num9 >= numArray[index])
                ++index;
            return part == 2 ? index : num9 - numArray[index - 1] + 1;
        }

        internal void GetDatePart(out int year, out int month, out int day) {
            int num1 = (int) (Ticks / 864000000000L);
            int num2 = num1 / 146097;
            int num3 = num1 - num2 * 146097;
            int num4 = num3 / 36524;
            if (num4 == 4)
                num4 = 3;
            int num5 = num3 - num4 * 36524;
            int num6 = num5 / 1461;
            int num7 = num5 - num6 * 1461;
            int num8 = num7 / 365;
            if (num8 == 4)
                num8 = 3;
            year = num2 * 400 + num4 * 100 + num6 * 4 + num8 + 1;
            int num9 = num7 - num8 * 365;
            int[] numArray = num8 == 3 && (num6 != 24 || num4 == 3) ? Constants.DaysToMonth366 : Constants.DaysToMonth365;
            int index = (num9 >> 5) + 1;
            while (num9 >= numArray[index])
                ++index;
            month = index;
            day = num9 - numArray[index - 1] + 1;
        }

        #endregion
    }
}