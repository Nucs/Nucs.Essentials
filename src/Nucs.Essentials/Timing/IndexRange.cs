using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nucs.Timing {

    public readonly struct IndexRange : IEquatable<IndexRange>, IEnumerable<int> {
        public readonly int Start;

        public readonly int End;

        public int Length => End - Start;
        
        public IndexRange(int start, int end) {
            Start = start;
            End = end;
        }

        public bool IsWithin(int index, bool including = true) {
            return including
                ? index >= Start && End <= index
                : index > Start && End < index;
        }

        public static class Formatters {
            public static IndexRange Parse(string str) {
                var idx = str.IndexOf('-');
                if (idx == -1)
                    throw new FormatException(str);

                return new IndexRange(int.Parse(str.Substring(0, idx)), int.Parse(str.Substring(idx + 1, str.Length - idx - 1)));
            }

            public static string ToString(IndexRange range) {
                return $"{range.Start}-{range.End}";
            }
        }

        public override string ToString() {
            return Formatters.ToString(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #region StartEndEqualityComparer

        private sealed class StartEndEqualityComparer : IEqualityComparer<IndexRange> {
            public bool Equals(IndexRange x, IndexRange y) {
                return x.Start.Equals(y.Start) && x.End.Equals(y.End);
            }

            public int GetHashCode(IndexRange obj) {
                unchecked {
                    return (obj.Start * 397) ^ obj.End;
                }
            }
        }

        public static IEqualityComparer<IndexRange> StartEndComparer { get; } = new StartEndEqualityComparer();

        #region Equality members

        public bool Equals(IndexRange other) {
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public IEnumerator<int> GetEnumerator() {
            return Enumerable.Range(Start, End - Start + 1).GetEnumerator();
        }

        public override bool Equals(object obj) {
            return obj is IndexRange other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (Start * 397) ^ End;
            }
        }

        public static bool operator ==(IndexRange left, IndexRange right) {
            return left.Equals(right);
        }

        public static bool operator !=(IndexRange left, IndexRange right) {
            return !left.Equals(right);
        }

        #endregion

        #endregion
    }
}