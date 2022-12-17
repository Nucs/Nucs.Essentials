using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nucs {
    public sealed class AscendingComparer<T> : IComparer<T> where T : IComparable<T> {
        public static readonly AscendingComparer<T> Default = new();

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y) {
            return x.CompareTo(y);
        }
    }
}