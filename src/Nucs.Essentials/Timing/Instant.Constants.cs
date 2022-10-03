namespace Nucs.Timing {
    public readonly partial struct Instant {
        /// <summary>Represents the zero <see cref="Instant" /> value. This field is read-only.</summary>
        public static readonly Instant Zero = new Instant(0L);

        /// <summary>Represents the maximum <see cref="Instant" /> value. This field is read-only.</summary>
        public static readonly Instant MaxValue = new Instant(long.MaxValue);

        /// <summary>Represents the minimum <see cref="Instant" /> value. This field is read-only.</summary>
        public static readonly Instant MinValue = new Instant(long.MinValue);

        public static class Constants {
            /// <summary>Represents the number of ticks in 1 millisecond. This field is constant.</summary>
            public const long TicksPerMillisecond = 10000;

            public const double MillisecondsPerTick = 0.0001;

            /// <summary>Represents the number of ticks in 1 second.</summary>
            public const long TicksPerSecond = 10000000;

            public const double SecondsPerTick = 1E-07;

            /// <summary>Represents the number of ticks in 1 minute. This field is constant.</summary>
            public const long TicksPerMinute = 600000000;

            public const double MinutesPerTick = 1.66666666666667E-09;

            /// <summary>Represents the number of ticks in 1 hour. This field is constant.</summary>
            public const long TicksPerHour = 36000000000;

            public const double HoursPerTick = 2.77777777777778E-11;

            /// <summary>Represents the number of ticks in 1 day. This field is constant.</summary>
            public const long TicksPerDay = 864000000000L;

            public const double DaysPerTick = 1.15740740740741E-12;
            public const int MillisPerSecond = 1000;
            public const int MillisPerMinute = 60000;
            public const int MillisPerHour = 3600000;
            public const int MillisPerDay = 86400000;
            public const long MaxSeconds = 922337203685;
            public const long MinSeconds = -922337203685;
            public const long MaxMilliSeconds = 922337203685477;
            public const long MinMilliSeconds = -922337203685477;
            public const long TicksPerTenthSecond = 1000000;
            public const int DaysPerYear = 365;
            public const int DaysPer4Years = 1461;
            public const int DaysPer100Years = 36524;
            public const int DaysPer400Years = 146097;
            public const int DaysTo1601 = 584388;
            public const int DaysTo1899 = 693593;
            public const int DaysTo1970 = 719162;
            public const int DaysTo10000 = 3652059;
            public const long MinTicks = 0;
            public const long MaxTicks = 3155378975999999999;
            public const long MaxMillis = 315537897600000;
            public const long FileTimeOffset = 504911232000000000;
            public const long DoubleDateOffset = 599264352000000000;
            public const long OADateMinAsTicks = 31241376000000000;
            public const double OADateMinAsDouble = -657435.0;
            public const double OADateMaxAsDouble = 2958466.0;
            public const int DatePartYear = 0;
            public const int DatePartDayOfYear = 1;
            public const int DatePartMonth = 2;
            public const int DatePartDay = 3;


            public static readonly int[] DaysToMonth365 = new int[13] {
                0,
                31,
                59,
                90,
                120,
                151,
                181,
                212,
                243,
                273,
                304,
                334,
                365
            };

            public static readonly int[] DaysToMonth366 = new int[13] {
                0,
                31,
                60,
                91,
                121,
                152,
                182,
                213,
                244,
                274,
                305,
                335,
                366
            };
        }
    }
}