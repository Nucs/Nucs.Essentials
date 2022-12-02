using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nucs.Collections;

internal static class SpanHelper {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BinarySearch<T, TComparable>(
        this ReadOnlySpan<T> span, TComparable comparable)
        where TComparable : IComparable<T> {
        if (comparable == null)
            throw new ArgumentNullException(nameof(comparable), "comparable cannot be null");
        return BinarySearch(ref MemoryMarshal.GetReference(span), span.Length, comparable);
    }

    public static int BinarySearch<T, TComparable>(
        ref T spanStart, int length, TComparable comparable)
        where TComparable : IComparable<T> {
        int lo = 0;
        int hi = length - 1;
        // If length == 0, hi == -1, and loop will not be entered
        while (lo <= hi) {
            // PERF: `lo` or `hi` will never be negative inside the loop,
            //       so computing median using uints is safe since we know
            //       `length <= int.MaxValue`, and indices are >= 0
            //       and thus cannot overflow an uint.
            //       Saves one subtraction per loop compared to
            //       `int i = lo + ((hi - lo) >> 1);`
            int i = (int) (((uint) hi + (uint) lo) >> 1);

            int c = comparable.CompareTo(Unsafe.Add(ref spanStart, i));
            if (c == 0) {
                return i;
            } else if (c > 0) {
                lo = i + 1;
            } else {
                hi = i - 1;
            }
        }

        // If none found, then a negative number that is the bitwise complement
        // of the index of the next element that is larger than or, if there is
        // no larger element, the bitwise complement of `length`, which
        // is `lo` at this point.
        return ~lo;
    }

    public static int BinarySearch<TSpan, TComparable>(
        ref TSpan spanStart, int length, TComparable comparable, OrderByDelegate<TSpan, TComparable> selector)
        where TComparable : IComparable<TComparable> {
        int lo = 0;
        int hi = length - 1;
        // If length == 0, hi == -1, and loop will not be entered
        while (lo <= hi) {
            // PERF: `lo` or `hi` will never be negative inside the loop,
            //       so computing median using uints is safe since we know
            //       `length <= int.MaxValue`, and indices are >= 0
            //       and thus cannot overflow an uint.
            //       Saves one subtraction per loop compared to
            //       `int i = lo + ((hi - lo) >> 1);`
            int i = (int) (((uint) hi + (uint) lo) >> 1);

            int c = comparable.CompareTo(selector(Unsafe.Add(ref spanStart, i)));
            if (c == 0) {
                return i;
            } else if (c > 0) {
                lo = i + 1;
            } else {
                hi = i - 1;
            }
        }

        // If none found, then a negative number that is the bitwise complement
        // of the index of the next element that is larger than or, if there is
        // no larger element, the bitwise complement of `length`, which
        // is `lo` at this point.
        return ~lo;
    }

    public static int BinarySearch<TSpan, TComparable, TComparer>(
        ref TSpan spanStart, int length, TComparable comparable, OrderByDelegate<TSpan, TComparable> selector, TComparer comparer)
        where TComparer : IComparer<TComparable> {
        int lo = 0;
        int hi = length - 1;
        // If length == 0, hi == -1, and loop will not be entered
        while (lo <= hi) {
            // PERF: `lo` or `hi` will never be negative inside the loop,
            //       so computing median using uints is safe since we know
            //       `length <= int.MaxValue`, and indices are >= 0
            //       and thus cannot overflow an uint.
            //       Saves one subtraction per loop compared to
            //       `int i = lo + ((hi - lo) >> 1);`
            int i = (int) (((uint) hi + (uint) lo) >> 1);

            int c = comparer.Compare(comparable, selector(Unsafe.Add(ref spanStart, i)));
            if (c == 0) {
                return i;
            } else if (c > 0) {
                lo = i + 1;
            } else {
                hi = i - 1;
            }
        }

        // If none found, then a negative number that is the bitwise complement
        // of the index of the next element that is larger than or, if there is
        // no larger element, the bitwise complement of `length`, which
        // is `lo` at this point.
        return ~lo;
    }

    // Helper to allow sharing all code via IComparable<T> inlineable
    internal readonly struct ComparerComparable<T, TComparer> : IComparable<T>
        where TComparer : IComparer<T> {
        private readonly T _value;
        private readonly TComparer _comparer;

        public ComparerComparable(T value, TComparer comparer) {
            _value = value;
            _comparer = comparer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(T? other) =>
            _comparer.Compare(_value, other);
    }
}