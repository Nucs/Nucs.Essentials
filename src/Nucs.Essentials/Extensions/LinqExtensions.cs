using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nucs.Extensions {
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/>
    /// </summary>
    public static class LinqExtensions {
        /// <summary>
        /// Applies the action to each element in the list.
        /// </summary>
        /// <typeparam name="T">The enumerable item's type.</typeparam>
        /// <param name="enumerable">The elements to enumerate.</param>
        /// <param name="action">The action to apply to each item in the list.</param>
        public static void Apply<T>(this IEnumerable<T> enumerable, Action<T> action) {
            foreach (var item in enumerable) {
                action(item);
            }
        }

        public static TElement[] ToArrayFast<TElement>(this IEnumerable<TElement> next, int? knownSize = null) {
            if (next == null)
                return Array.Empty<TElement>();

            TElement[] array = null;
            int length = 0;
            foreach (TElement element in next) {
                if (array == null)
                    array = new TElement[knownSize ?? 4];
                else if (array.Length == length) {
                    TElement[] elementArray = new TElement[checked(length * 4L)];
                    Array.Copy(array, 0, elementArray, 0, length);
                    array = elementArray;
                }

                array[length++] = element;
            }

            if (array == null)
                return Array.Empty<TElement>();

            if (array.Length != length)
                Array.Resize(ref array, length);

            return array;
        }

        public static TElement[] ToArrayFast<TElement>(this List<TElement> next, int? knownSize = null) {
            if (next == null)
                return Array.Empty<TElement>();

            TElement[] array = null;
            int length = 0;
            foreach (TElement element in next) {
                if (array == null)
                    array = new TElement[knownSize ?? 4];
                else if (array.Length == length) {
                    TElement[] elementArray = new TElement[checked(length * 4L)];
                    Array.Copy(array, 0, elementArray, 0, length);
                    array = elementArray;
                }

                array[length++] = element;
            }

            if (array == null)
                return Array.Empty<TElement>();

            if (array.Length != length)
                Array.Resize(ref array, length);

            return array;
        }

        public static List<TElement> ToListFast<TElement>(this IEnumerable<TElement> next, int? knownSize = null) {
            if (next == null)
                return new List<TElement>(0);

            List<TElement> array = new List<TElement>(knownSize ?? 32);
            array.AddRange(next);

            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T? FindType<T, TBase>(this IEnumerable<TBase> collection) where T : TBase {
            foreach (var col in collection) {
                if (col is T res)
                    return res;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T? FindType<T, TBase>(this IList<TBase> collection) where T : TBase {
            if (collection.Count == 1) {
                if (collection[0] is T res) {
                    return res;
                }

                return default;
            }

            for (var i = 0; i < collection.Count; i++) {
                if (collection[i] is T res)
                    return res;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T? FindType<T, TBase>(this List<TBase> collection) where T : TBase {
            if (collection.Count == 1) {
                if (collection[0] is T res) {
                    return res;
                }

                return default;
            }

            foreach (var col in collection) {
                if (col is T res)
                    return res;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static IEnumerable<T> FindTypes<T, TBase>(this IEnumerable<TBase> collection) where T : TBase {
            foreach (var col in collection) {
                if (col is T res)
                    yield return res;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static IEnumerable<T> FindTypes<T, TBase>(this IList<TBase> collection) where T : TBase {
            foreach (var col in collection) {
                if (col is T res)
                    yield return res;
            }
        }
    }
}