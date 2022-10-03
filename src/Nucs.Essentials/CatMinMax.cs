using System;
using System.Collections.Generic;

namespace Nucs {
    public class CatMinMaxComparer<T> : IComparer<CatMinMax<T>> where T : IComparable<T> {
        public int Compare(CatMinMax<T> x, CatMinMax<T> y) {
            // TODO: Handle x or y being null, or them not having names
            return x.Min.CompareTo(y.Min);
        }
    }

    public readonly struct CatMinMax<T> : IEquatable<CatMinMax<T>>, IComparable where T : IComparable<T> {
        public readonly T Min;
        public readonly T Max;

        public CatMinMax(T i, T j) {
            Min = i;
            Max = j;
        }

        public override string ToString() {
            return $"{nameof(Min)}: {Min}, {nameof(Max)}: {Max}";
        }

        public bool Contains(T c) {
            int cMin = Compare(c, Min);

            if (cMin < 0)
                return false;

            int cMax = Compare(Max, c);

            if (cMax < 0)
                return false;

            return true;
        }


        public string ToCSV() {
            return Min.ToString() + "," + Max.ToString();
        }

        #region Equality members

        public bool Equals(CatMinMax<T> other) {
            return EqualityComparer<T>.Default.Equals(Min, other.Min) && EqualityComparer<T>.Default.Equals(Max, other.Max);
        }

        public override bool Equals(object obj) {
            return obj is CatMinMax<T> other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                return (EqualityComparer<T>.Default.GetHashCode(Min) * 397) ^ EqualityComparer<T>.Default.GetHashCode(Max);
            }
        }

        public static bool operator ==(CatMinMax<T> left, CatMinMax<T> right) {
            return left.Equals(right);
        }

        public static bool operator !=(CatMinMax<T> left, CatMinMax<T> right) {
            return !left.Equals(right);
        }

        #endregion

        public int Compare(T a, T b) {
            return a.CompareTo(b);
        }

        int IComparable.CompareTo(object obj) {
            CatMinMax<T> c = (CatMinMax<T>) obj;
            return Compare(this.Min, c.Min);
        }
    }

    public static class CatMinMaxHelper {
        public static void CreateMinMaxCategories(List<long> listCategory, List<long> listValues, ref Dictionary<CatMinMax<long>, long> sortedDictionaryCategory) {
            for (int i = 0; i < listCategory.Count - 1; i++) {
                long minVal = listCategory[i];
                long maxVal = listCategory[i + 1];

                long val = listValues[i];

                CatMinMax<long> minMax = new CatMinMax<long>(minVal, maxVal);

                sortedDictionaryCategory.Add(minMax, val);
            }
        }

        public static void CreateMinMaxCategories(List<long> listCategory, List<string> listValues, ref Dictionary<CatMinMax<long>, string> sortedDictionaryCategory, bool uselastAsMax = false) {
            for (int i = 0; i < listCategory.Count - 1; i++) {
                long minVal = listCategory[i];

                long maxVal = listCategory[i + 1];
                string val = string.Empty;

                if (uselastAsMax) {
                    maxVal = listCategory[listCategory.Count - 1];
                    val = minVal.ToString();
                } else {
                    val = listValues[i];
                }

                CatMinMax<long> minMax = new CatMinMax<long>(minVal, maxVal);

                sortedDictionaryCategory.Add(minMax, val);
            }
        }
    }
}