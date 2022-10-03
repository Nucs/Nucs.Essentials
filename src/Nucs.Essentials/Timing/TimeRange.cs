using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Nucs.Configuration;

namespace Nucs.Timing {

    public readonly struct TimeRange : IEquatable<TimeRange> {
        public readonly DateTime Start;

        public readonly DateTime End;

        /// <summary>
        ///     End - Start;
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration => End - Start;

        /// <summary>
        ///     new TimeRange(Start.Date, End.Date)
        /// </summary>
        [JsonIgnore]
        public TimeRange Date => new TimeRange(Start.Date, End.Date);

        [JsonConstructor]
        public TimeRange(DateTime start, DateTime end) {
            Start = start;
            End = end;
        }

        /// <summary>
        ///     Example input: 20200205 for year 2020, month 02, day 05
        /// </summary>
        public TimeRange(int start, int end) {
            Start = new DateTime(start / 1_0000, (start / 100) % 100, start % 100);
            End = new DateTime(end / 1_0000, (end / 100) % 100, end % 100);
        }

        public TimeRange(DateTime start, TimeSpan duration) {
            Start = start;
            End = start.Add(duration);
        }

        public static TimeRange EntireDay(DateTime date) {
            return new TimeRange(date.Date, date.Date.AddTicks(Instant.Constants.TicksPerDay - Instant.Constants.TicksPerMillisecond));
        }

        /// <summary>
        ///     Example input: 20200205 for year 2020, month 02, day 05
        /// </summary>
        /// <param name="date">20200205 for year 2020, month 02, day 05</param>
        public static TimeRange EntireDay(int date) {
            var dateTime = new DateTime(date / 1_0000, (date / 100) % 100, date % 100);
            return new TimeRange(dateTime, dateTime.AddTicks(Instant.Constants.TicksPerDay - Instant.Constants.TicksPerMillisecond));
        }

        public bool Contains(DateTime time, bool including = true) {
            if (time == default)
                return true;
            if (this == default)
                return false;
            return including
                ? time >= Start && End >= time
                : time > Start && End > time;
        }

        public bool Contains(TimeRange time, bool including = true) {
            if (time == default)
                return true;
            if (this == default)
                return false;
            return including
                ? time.Start >= Start && End >= time.End
                : time.Start > Start && End > time.End;
        }

        public bool Intersects(TimeRange time, bool including = true) {
            if (time == default)
                return true;
            if (this == default)
                return false;
            return including
                ? (time.Start >= Start && End >= time.Start) || (time.End >= Start && End >= time.End)
                : (time.Start > Start && End > time.Start) || (time.End > Start && End > time.End);
        }

        public static (TimeRange LeftOrSingleSide, TimeRange? RightSide) operator -(TimeRange range, TimeRange remove) {
            if (range.Contains(remove)) {
                return (new TimeRange(range.Start, remove.Start), new TimeRange(remove.End, range.End));
            } else if (remove.Contains(range.Start)) {
                return (new TimeRange(remove.End, range.End), default);
            } else if (remove.Contains(range.End)) {
                return (new TimeRange(range.Start, remove.Start), default);
            } else {
                //range is unaffected
                return (range, default);
            }
        }

        public static TimeRange operator +(TimeRange range, TimeRange add) {
            if (range == default)
                return add;
            if (add == default)
                return range;
            return new TimeRange(new DateTime(Math.Min(range.Start.Ticks, add.Start.Ticks)), new DateTime(Math.Min(range.End.Ticks, add.End.Ticks)));
        }

        public static class Formatters {
            public static TimeRange Parse(string str) {
                var idx = str.IndexOf('-');
                if (idx == -1)
                    throw new FormatException(str);

                return new TimeRange(ConfigParsers.DateTime(str.Substring(0, idx)), ConfigParsers.DateTime(str.Substring(idx + 1, str.Length - idx - 1)));
            }

            public static string ToString(TimeRange range) {
                if (range.Date == range)
                    return $"{range.Start:yyyyMMdd}-{range.End:yyyyMMdd}";
                return $"{range.Start:yyyyMMdd@HH:mm:ss}-{range.End:yyyyMMdd@HH:mm:ss}";
            }
        }

        public override string ToString() {
            return Formatters.ToString(this);
        }

        public Enumerator GetDaysEnumerator() {
            return new Enumerator(this.Start, this.End);
        }

        public IEnumerable<DateTime> GetDays() {
            using var enumerator = new Enumerator(this.Start, this.End);
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }


        #region StartEndEqualityComparer

        private sealed class StartEndEqualityComparer : IEqualityComparer<TimeRange> {
            public bool Equals(TimeRange x, TimeRange y) {
                return x.Start.Equals(y.Start) && x.End.Equals(y.End);
            }

            public int GetHashCode(TimeRange obj) {
                unchecked {
                    return (obj.Start.GetHashCode() * 397) ^ obj.End.GetHashCode();
                }
            }
        }

        public static IEqualityComparer<TimeRange> StartEndComparer { get; } = new StartEndEqualityComparer();

        #region Equality members

        public bool Equals(TimeRange other) {
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override bool Equals(object obj) {
            return obj is TimeRange other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        public static bool operator ==(TimeRange left, TimeRange right) {
            return left.Equals(right);
        }

        public static bool operator !=(TimeRange left, TimeRange right) {
            return !left.Equals(right);
        }

        #endregion

        #endregion

        public struct Enumerator : IEnumerator<DateTime> {
            private readonly DateTime _start;
            private readonly DateTime _end;
            private DateTime _current;
            private byte _state;

            public Enumerator(DateTime start, int count) {
                Debug.Assert(count > 0);
                _start = start;
                _end = start.AddDays(count);
                if (_start == _end)
                    _end = _end.AddDays(1);
                _current = default;
                _state = 0;
            }

            public Enumerator(DateTime start, DateTime end) {
                Debug.Assert(end >= start);
                _start = start.Date;
                _end = end.Date;
                if (_start == _end)
                    _end = _end.AddDays(1);
                _current = default;
                _state = 0;
            }

            public Enumerator Clone() =>
                new Enumerator(_start, (int) Math.Round((_end - _start).TotalDays));

            public bool MoveNext() {
                switch (_state) {
                    case 0:
                        Debug.Assert(_start != _end);
                        _current = _start;
                        _state = 1;
                        return true;
                    case 1:
                        _current = _current.AddDays(1);
                        if (_current == _end) {
                            break;
                        }

                        return true;
                }

                _state = 255;
                return false;
            }

            public void Reset() {
                _current = default;
                _state = 255;
            }

            public DateTime Current => _current;

            object IEnumerator.Current => Current;

            public void Dispose() {
                _state = 255; // Don't reset current
            }
        }
    }
}