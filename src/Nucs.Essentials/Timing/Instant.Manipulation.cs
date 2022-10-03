using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Nucs.Timing {
    public readonly partial struct Instant {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToDate(long time) =>
            time - ToTimeOfDay(time); //to date

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToTimeOfDay(long time) =>
            unchecked(time % Instant.Constants.TicksPerDay); //to time of day

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Instant Add(Instant value) =>
            new Instant(checked(Ticks + value.Ticks));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Instant Add(DateTime value) =>
            new Instant(checked(Ticks + value.Ticks));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Instant Add(TimeSpan value) =>
            new Instant(checked(Ticks + value.Ticks));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Instant Add(long value) =>
            new Instant(checked(Ticks + value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Instant Add(int value) =>
            new Instant(checked(Ticks + value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Instant Add(byte value) =>
            new Instant(checked(Ticks + value));

        /// <summary>Returns a new <see cref="T:System.DateTime" /> that adds the value of the specified <see cref="T:System.TimeSpan" /> to the value of this instance.</summary>
        /// <param name="value">A positive or negative time interval.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the time interval represented by <paramref name="value" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The resulting <see cref="T:System.DateTime" /> is less than <see cref="F:System.DateTime.MinValue" /> or greater than <see cref="F:System.DateTime.MaxValue" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long Add(double value, int scale) {
            return Ticks + ((long) (value * scale + (value >= 0.0 ? 0.5 : -0.5)) * 10000L);
        }

        /// <summary>Returns a new <see cref="T:System.Instant" /> that adds the specified number of days to the value of this instance.</summary>
        /// <param name="value">A number of whole and fractional days. The <paramref name="value" /> parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of days represented by <paramref name="value" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The resulting <see cref="T:System.Instant" /> is less than <see cref="F:System.Instant.MinValue" /> or greater than <see cref="F:System.Instant.MaxValue" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Instant AddDays(double value) =>
            this.Add(value, 86400000);

        /// <summary>Returns a new <see cref="T:System.Instant" /> that adds the specified number of hours to the value of this instance.</summary>
        /// <param name="value">A number of whole and fractional hours. The <paramref name="value" /> parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of hours represented by <paramref name="value" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The resulting <see cref="T:System.Instant" /> is less than <see cref="F:System.Instant.MinValue" /> or greater than <see cref="F:System.Instant.MaxValue" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Instant AddHours(double value) =>
            this.Add(value, 3600000);

        /// <summary>Returns a new <see cref="T:System.Instant" /> that adds the specified number of milliseconds to the value of this instance.</summary>
        /// <param name="value">A number of whole and fractional milliseconds. The <paramref name="value" /> parameter can be negative or positive. Note that this value is rounded to the nearest integer.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of milliseconds represented by <paramref name="value" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The resulting <see cref="T:System.Instant" /> is less than <see cref="F:System.Instant.MinValue" /> or greater than <see cref="F:System.Instant.MaxValue" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Instant AddMilliseconds(double value) =>
            this.Add(value, 1);

        /// <summary>Returns a new <see cref="T:System.Instant" /> that adds the specified number of minutes to the value of this instance.</summary>
        /// <param name="value">A number of whole and fractional minutes. The <paramref name="value" /> parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of minutes represented by <paramref name="value" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The resulting <see cref="T:System.Instant" /> is less than <see cref="F:System.Instant.MinValue" /> or greater than <see cref="F:System.Instant.MaxValue" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Instant AddMinutes(double value) =>
            this.Add(value, 60000);

        /// <summary>Returns a new <see cref="T:System.Instant" /> that adds the specified number of months to the value of this instance.</summary>
        /// <param name="months">A number of months. The <paramref name="months" /> parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and <paramref name="months" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The resulting <see cref="T:System.Instant" /> is less than <see cref="F:System.Instant.MinValue" /> or greater than <see cref="F:System.Instant.MaxValue" />.
        /// -or-
        /// <paramref name="months" /> is less than -120,000 or greater than 120,000.</exception>
        public Instant AddMonths(int months) {
            if (months < -120000 || months > 120000)
                throw new ArgumentOutOfRangeException(nameof(months), "ArgumentOutOfRange_InstantBadMonths");
            int year1;
            int month;
            int day;
            this.GetDatePart(out year1, out month, out day);
            int num1 = month - 1 + months;
            int year2;
            if (num1 >= 0) {
                month = num1 % 12 + 1;
                year2 = year1 + num1 / 12;
            } else {
                month = 12 + (num1 + 1) % 12;
                year2 = year1 + (num1 - 11) / 12;
            }

            int num2 = year2 >= 1 && year2 <= 9999 ? DateTime.DaysInMonth(year2, month) : throw new ArgumentOutOfRangeException(nameof(months), "ArgumentOutOfRange_DateArithmetic");
            if (day > num2)
                day = num2;
            return new Instant((long) (DateToTicks(year2, month, day) + Ticks % 864000000000L));
        }

        /// <summary>Returns a new <see cref="T:System.Instant" /> that adds the specified number of seconds to the value of this instance.</summary>
        /// <param name="value">A number of whole and fractional seconds. The <paramref name="value" /> parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of seconds represented by <paramref name="value" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The resulting <see cref="T:System.Instant" /> is less than <see cref="F:System.Instant.MinValue" /> or greater than <see cref="F:System.Instant.MaxValue" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Instant AddSeconds(double value) =>
            this.Add(value, 1000);

        /// <summary>Returns a new <see cref="T:System.Instant" /> that adds the specified number of ticks to the value of this instance.</summary>
        /// <param name="value">A number of 100-nanosecond ticks. The <paramref name="value" /> parameter can be positive or negative.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the time represented by <paramref name="value" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The resulting <see cref="T:System.Instant" /> is less than <see cref="F:System.Instant.MinValue" /> or greater than <see cref="F:System.Instant.MaxValue" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Instant AddTicks(long value) {
            return new Instant(Ticks + value);
        }

        /// <summary>Returns a new <see cref="T:System.Instant" /> that adds the specified number of years to the value of this instance.</summary>
        /// <param name="value">A number of years. The <paramref name="value" /> parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of years represented by <paramref name="value" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="value" /> or the resulting <see cref="T:System.Instant" /> is less than <see cref="F:System.Instant.MinValue" /> or greater than <see cref="F:System.Instant.MaxValue" />.</exception>
        public Instant AddYears(int value) {
            return this.AddMonths(value * 12);
        }

        [JsonIgnore]
        public Instant TimeOfDay => new Instant(Instant.ToTimeOfDay(this.Ticks));
    }
}