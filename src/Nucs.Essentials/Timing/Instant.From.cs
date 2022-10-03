using System;

namespace Nucs.Timing {
    public readonly partial struct Instant {
        /// <summary>Returns a <see cref="T:System.TimeSpan" /> that represents a specified time, where the specification is in units of ticks.</summary>
        /// <param name="value">A number of ticks that represent a time.</param>
        /// <returns>An object that represents <paramref name="value" />.</returns>
        public static Instant FromTicks(long value) =>
            new TimeSpan(value);

        /// <summary>Returns a <see cref="T:System.TimeSpan" /> that represents a specified number of milliseconds.</summary>
        /// <param name="value">A number of milliseconds.</param>
        /// <returns>An object that represents <paramref name="value" />.</returns>
        /// <exception cref="T:System.OverflowException">
        ///         <paramref name="value" /> is less than <see cref="F:System.TimeSpan.MinValue" /> or greater than <see cref="F:System.TimeSpan.MaxValue" />.
        /// -or-
        /// <paramref name="value" /> is <see cref="F:System.Double.PositiveInfinity" />.
        /// -or-
        /// <paramref name="value" /> is <see cref="F:System.Double.NegativeInfinity" />.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> is equal to <see cref="F:System.Double.NaN" />.</exception>
        public static Instant FromMilliseconds(double value) =>
            Interval(value, 1);

        /// <summary>Returns a <see cref="T:System.TimeSpan" /> that represents a specified number of minutes, where the specification is accurate to the nearest millisecond.</summary>
        /// <param name="value">A number of minutes, accurate to the nearest millisecond.</param>
        /// <returns>An object that represents <paramref name="value" />.</returns>
        /// <exception cref="T:System.OverflowException">
        ///         <paramref name="value" /> is less than <see cref="F:System.TimeSpan.MinValue" /> or greater than <see cref="F:System.TimeSpan.MaxValue" />.
        /// -or-
        /// <paramref name="value" /> is <see cref="F:System.Double.PositiveInfinity" />.
        /// -or-
        /// <paramref name="value" /> is <see cref="F:System.Double.NegativeInfinity" />.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> is equal to <see cref="F:System.Double.NaN" />.</exception>
        public static Instant FromMinutes(double value) =>
            Interval(value, 60000);

        private static Instant Interval(double value, int scale) {
            double num = value * scale + (value >= 0.0 ? 0.5 : -0.5);
            return num <= 922337203685477.0 && num >= -922337203685477.0 ? new Instant((long) num * 10000L) : throw new OverflowException("Overflow_TimeSpanTooLong");
        }

        /// <summary>Returns a new <see cref="T:System.TimeSpan" /> object whose value is the negated value of this instance.</summary>
        /// <returns>A new object with the same numeric value as this instance, but with the opposite sign.</returns>
        /// <exception cref="T:System.OverflowException">The negated value of this instance cannot be represented by a <see cref="T:System.TimeSpan" />; that is, the value of this instance is <see cref="F:System.TimeSpan.MinValue" />.</exception>
        public Instant Negate() {
            if (Ticks == MinValue.Ticks)
                throw new OverflowException("Overflow_NegateTwosCompNum");
            return new Instant(-Ticks);
        }

        /// <summary>Returns a <see cref="T:System.TimeSpan" /> that represents a specified number of seconds, where the specification is accurate to the nearest millisecond.</summary>
        /// <param name="value">A number of seconds, accurate to the nearest millisecond.</param>
        /// <returns>An object that represents <paramref name="value" />.</returns>
        /// <exception cref="T:System.OverflowException">
        ///         <paramref name="value" /> is less than <see cref="F:System.TimeSpan.MinValue" /> or greater than <see cref="F:System.TimeSpan.MaxValue" />.
        /// -or-
        /// <paramref name="value" /> is <see cref="F:System.Double.PositiveInfinity" />.
        /// -or-
        /// <paramref name="value" /> is <see cref="F:System.Double.NegativeInfinity" />.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="value" /> is equal to <see cref="F:System.Double.NaN" />.</exception>
        public static Instant FromSeconds(double value) =>
            Interval(value, 1000);
    }
}