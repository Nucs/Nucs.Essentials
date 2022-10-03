using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Nucs.Collections.Layouts;
using Nucs.Collections.Structs;

namespace Nucs.Extensions {
    public static class Collections {
        /// <summary>
        ///     Expands this array by adding <paramref name="item"/> to the end.
        /// </summary>
        public static T[] Expand<T>(this T[] arr, T item) {
            if (arr == null || arr.Length == 0)
                return new T[] { item };
            Array.Resize(ref arr, arr.Length + 1);
            arr[arr.Length - 1] = item;
            return arr;
        }

        /// <summary>
        ///     Iterates each <see cref="buckets"/> item, if its bigger than <see cref="referenceValue"/> then <see cref="values"/> of same index will be returned.
        /// </summary>
        /// <param name="buckets"></param>
        /// <param name="values"></param>
        /// <param name="referenceValue"></param>
        /// <param name="inclusive">Include the bucket upper threshold in the lookup for the bucket group</param>
        /// <returns><see cref="values"/> of an index with a <see cref="buckets"/> larger than <see cref="values"/></returns>
        public static double GetBucketValue(this List<double> buckets, List<double> values, double referenceValue, bool inclusive = false) {
            if (inclusive)
                for (int i = 0; i < buckets.Count; i++) {
                    if (referenceValue <= buckets[i] || i == buckets.Count - 1) {
                        return values[i];
                    }
                }
            else
                for (int i = 0; i < buckets.Count; i++) {
                    if (referenceValue < buckets[i] || i == buckets.Count - 1) {
                        return values[i];
                    }
                }

            //never called
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Iterates each <see cref="buckets"/> item, if its bigger than <see cref="referenceValue"/> then <see cref="values"/> of same index will be returned.
        /// </summary>
        /// <param name="buckets"></param>
        /// <param name="values"></param>
        /// <param name="referenceValue"></param>
        /// <param name="inclusive">Include the bucket upper threshold in the lookup for the bucket group</param>
        /// <returns><see cref="values"/> of an index with a <see cref="buckets"/> larger than <see cref="values"/></returns>
        public static double GetBucketValue(this double[] buckets, double[] values, double referenceValue, bool inclusive = false) {
            if (inclusive)
                for (int i = 0; i < buckets.Length; i++) {
                    if (referenceValue <= buckets[i] || i == buckets.Length - 1) {
                        return values[i];
                    }
                }
            else
                for (int i = 0; i < buckets.Length; i++) {
                    if (referenceValue < buckets[i] || i == buckets.Length - 1) {
                        return values[i];
                    }
                }

            //never called
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Iterates each <see cref="buckets"/> item, if its bigger than <see cref="referenceValue"/> then <see cref="values"/> of same index will be returned.
        /// </summary>
        /// <param name="buckets"></param>
        /// <param name="values"></param>
        /// <param name="referenceValue"></param>
        /// <param name="inclusive">Include the bucket upper threshold in the lookup for the bucket group</param>
        /// <returns><see cref="values"/> of an index with a <see cref="buckets"/> larger than <see cref="values"/></returns>
        public static double[] GetBucketValue(this double[] buckets, double[][] values, double referenceValue, bool inclusive = false) {
            if (inclusive)
                for (int i = 0; i < buckets.Length; i++) {
                    if (referenceValue <= buckets[i] || i == buckets.Length - 1) {
                        return values[i];
                    }
                }
            else
                for (int i = 0; i < buckets.Length; i++) {
                    if (referenceValue < buckets[i] || i == buckets.Length - 1) {
                        return values[i];
                    }
                }

            //never called
            throw new InvalidOperationException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T SingleOrAt<T>(this List<T> list, int index) {
            if (list.Count == 1)
                return list[0];

            return list[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T SingleOrAt<T>(this List<T> list, short index) {
            if (list.Count == 1)
                return list[0];

            return list[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T SingleOrAt<T>(this List<T> list, byte index) {
            if (list.Count == 1)
                return list[0];

            return list[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static T SingleOrAt<T>(this T[] list, int index) {
            if (list.Length == 1)
                return list[0];

            return list[index];
        }

        /// <summary>
        ///     Wraps in a StructList
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static StructList<T> AsValueList<T>(this T[] array) {
            return new StructList<T>(array);
        }

        /// <summary>
        ///     Wraps in a StructList and unsafely casts the array to a different type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static StructList<T> AsValueListAs<T>(this Array array) {
            return new StructList<T>(Unsafe.As<Array, T[]>(ref array));
        }

        /// <summary>
        ///     Wraps in a StructList
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static StructList<T> AsStructList<T>(this T[] array) {
            return new StructList<T>(array);
        }

        /// <summary>
        ///     Wraps in a StructList
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static StructList<T> AsStructList<T>(this List<T> array) {
            return new StructList<T>(array.AsLayout()._items, array.Count);
        }

        /// <summary>
        ///     Wraps in a StructList and unsafely casts the array to a different type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static StructList<T> AsStructListAs<T>(this Array array) {
            return new StructList<T>(Unsafe.As<Array, T[]>(ref array));
        }
    }
}