using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nucs.Collections.Layouts {
    public static class LayoutExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ListLayout<T> AsLayout<T>(this List<T> list) {
            if (list is null) throw new ArgumentNullException(nameof(list));
            return Unsafe.As<List<T>, ListLayout<T>>(ref list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ListLayoutStruct<T> AsLayoutStruct<T>(this List<T> list) where T : unmanaged, IComparable {
            if (list is null) throw new ArgumentNullException(nameof(list));
            return Unsafe.As<List<T>, ListLayoutStruct<T>>(ref list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DictionaryLayout<TKey, TValue> AsLayout<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) {
            if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));
            return Unsafe.As<Dictionary<TKey, TValue>, DictionaryLayout<TKey, TValue>>(ref dictionary);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashsetLayout<T> AsLayout<T>(this HashSet<T> hashset) {
            if (hashset is null) throw new ArgumentNullException(nameof(hashset));
            return Unsafe.As<HashSet<T>, HashsetLayout<T>>(ref hashset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilderLayout AsLayout(this StringBuilder hashset) {
            if (hashset is null) throw new ArgumentNullException(nameof(hashset));
            return Unsafe.As<StringBuilder, StringBuilderLayout>(ref hashset);
        }
    }
}