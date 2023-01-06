using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Nucs.Configuration;

namespace Nucs.Timing {

    public readonly struct TimeSpanRange : IEquatable<TimeSpanRange> {
        public readonly TimeSpan Start;

        public readonly TimeSpan End;

        [JsonIgnore]
        public TimeSpan Duration => End - Start;

        public TimeSpanRange(TimeSpan start, TimeSpan end) {
            Start = start;
            End = end;
        }

        /// <summary>
        ///     Creates a random between Start and End, inclusive.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public TimeSpan Random(Random src) {
            return Start.Add(TimeSpan.FromTicks((long) (src.NextDouble() * (End - Start).Ticks)));
        }

        public bool IsWithin(DateTime time, bool including = true) {
            return IsWithin(time.TimeOfDay, including);
        }

        public bool IsWithin(TimeSpan time, bool including = true) {
            return including
                ? time >= Start && End >= time
                : time > Start && End > time;
        }

        public static class Formatters {
            public static TimeSpanRange Parse(string str) {
                var idx = str.IndexOf('-');
                if (idx == -1)
                    throw new FormatException(str);

                return new TimeSpanRange(ConfigParsers.TimeSpan(str.Substring(0, idx)), ConfigParsers.TimeSpan(str.Substring(idx + 1, str.Length - idx - 1)));
            }

            public static string ToString(TimeSpanRange range) {
                return $"{range.Start.ToString(AbstractInfrastructure.TimeFormatShort)}-{range.End.ToString(AbstractInfrastructure.TimeFormatShort)}";
            }

            public static string ToFilenameString(TimeSpanRange range) {
                return $"{range.Start.ToString(AbstractInfrastructure.TimeFilenameFormatShort)}-{range.End.ToString(AbstractInfrastructure.TimeFilenameFormatShort)}";
            }
        }

        public override string ToString() {
            return Formatters.ToString(this);
        }

        public string ToFilenameString() {
            return Formatters.ToFilenameString(this);
        }

        #region Equality members

        public bool Equals(TimeSpanRange other) {
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override bool Equals(object obj) {
            return obj is TimeSpanRange other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        public static bool operator ==(TimeSpanRange left, TimeSpanRange right) {
            return left.Equals(right);
        }

        public static bool operator !=(TimeSpanRange left, TimeSpanRange right) {
            return !left.Equals(right);
        }

        #endregion

        #region StartEndEqualityComparer

        private sealed class StartEndEqualityComparer : IEqualityComparer<TimeSpanRange> {
            public bool Equals(TimeSpanRange x, TimeSpanRange y) {
                return x.Start.Equals(y.Start) && x.End.Equals(y.End);
            }

            public int GetHashCode(TimeSpanRange obj) {
                unchecked {
                    return (obj.Start.GetHashCode() * 397) ^ obj.End.GetHashCode();
                }
            }
        }

        public static IEqualityComparer<TimeSpanRange> StartEndComparer { get; } = new StartEndEqualityComparer();

        #endregion
    }
}