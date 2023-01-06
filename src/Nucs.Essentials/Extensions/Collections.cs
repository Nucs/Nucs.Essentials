using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nucs.Collections.Layouts;
using Nucs.Collections.Structs;
#if NET6_0_OR_GREATER
using UnsafeHelper = System.Runtime.CompilerServices;
#else
using UnsafeHelper = Nucs.Extensions.UnsafeHelper;
#endif
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
        
        public static unsafe void Resize<T>(ref T[]? array, long newSize) {
            if (newSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newSize));

            T[]? src = array; // local copy
            if (src == null) {
                array = new T[newSize];
                return;
            }

            if (src.Length != newSize) {
                // Due to array variance, it's possible that the incoming array is
                // actually of type U[], where U:T; or that an int[] <-> uint[] or
                // similar cast has occurred. In any case, since it's always legal
                // to reinterpret U as T in this scenario (but not necessarily the
                // other way around), we can use Buffer.Memmove here.

                T[] dst = new T[newSize];
                
                #if NET6_0_OR_GREATER
                Buffer.MemoryCopy(
                    source: Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(src)),
                    destination: Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(dst)),
                    destinationSizeInBytes: newSize,
                    sourceBytesToCopy: Math.Min(src.LongLength, newSize));
                #else
                Buffer.MemoryCopy(
                    source: Unsafe.AsPointer(ref src[0]),
                    destination: Unsafe.AsPointer(ref dst[0]),
                    destinationSizeInBytes: newSize,
                    sourceBytesToCopy: Math.Min(src.LongLength, newSize));
                #endif
                array = dst;
            }

            Debug.Assert(array != null);
        }


        /// <summary>
        ///     Similar to <see cref="Array.Resize{T}"/>, just for a memory block created by <see cref="Marshal.AllocHGlobal(int)"/>.
        /// </summary>
        /// <param name="buffer">A pointer to a buffer allocated by <see cref="Marshal.AllocHGlobal(int)"/>. Can be <see cref="Unsafe.NullRef{T}"/></param>
        /// <param name="bufferSize">The length in bytes of the allocated pointer</param>
        /// <param name="newSize">The new length in requested in bytes</param>
        /// <param name="releasePreviousBuffer">Should the previous buffer be freed or just copied over</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static unsafe void ResizeMarshalled(ref byte* buffer, ref long bufferSize, long newSize, bool releasePreviousBuffer) {
            if (newSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(newSize));

            byte* src = buffer; // local copy
            if (UnsafeHelper.IsNullRef(ref Unsafe.AsRef<byte>(src))) {
                buffer = (byte*) Marshal.AllocHGlobal(new IntPtr(newSize));
                bufferSize = newSize;
                return;
            }

            if (bufferSize == newSize)
                return;

            if (releasePreviousBuffer) {
                buffer = (byte*) Marshal.ReAllocHGlobal(new IntPtr(src), new IntPtr(newSize));
                bufferSize = newSize;
            } else {
                byte* dst = (byte*) Marshal.AllocHGlobal(new IntPtr(newSize));
                Buffer.MemoryCopy(
                    source: src,
                    destination: dst,
                    destinationSizeInBytes: newSize,
                    sourceBytesToCopy: Math.Min(bufferSize, newSize));

                buffer = dst;
                bufferSize = newSize;
            }
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