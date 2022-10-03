using System;
using System.Runtime.CompilerServices;
using Nucs.Configuration;

namespace Nucs {
    public interface IPriceBucket {
        IPriceBucket New(Array buckets, Array values);
    }

    /// <summary>
    ///     Price bucket holding values to categories (buckets)
    /// </summary>
    public readonly struct PriceBucket<TBucket, TValue> : IPriceBucket where TBucket : unmanaged, IComparable<TBucket> {
        /// <summary>
        ///     The price groups aka buckets
        /// </summary>
        public readonly TBucket[] Buckets;

        /// <summary>
        ///     The values of the buckets.
        /// </summary>
        public readonly TValue[] Values;

        public PriceBucket(TBucket[] buckets, TValue[] values) {
            Buckets = buckets;
            Values = values;
            if (buckets.Length != values.Length)
                throw new ConfigurationException($"Values have {values.Length} items and Buckets have {buckets.Length} items. They must match.");
        }

        public readonly TValue this[TBucket referenceValue, bool inclusive = false] {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get { return Value(referenceValue, inclusive); }
        }

        /// <summary>
        ///     Resolves given 
        /// </summary>
        /// <param name="referenceValue"></param>
        /// <param name="inclusive"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public readonly TValue Value(TBucket referenceValue, bool inclusive = false) {
            //TODO:!@!@ test this CompareTo logic
            if (inclusive)
                for (int i = 0; i < Buckets.Length; i++) {
                    if (referenceValue.CompareTo(Buckets[i]) <= 0 || i == Buckets.Length - 1) {
                        return Values[i];
                    }
                }
            else
                for (int i = 0; i < Values.Length; i++) {
                    if (referenceValue.CompareTo(Buckets[i]) == -1 || i == Values.Length - 1) {
                        return Values[i];
                    }
                }

            //never called
            throw new InvalidOperationException();
        }


        public readonly IPriceBucket New(Array buckets, Array values) {
            return new PriceBucket<TBucket, TValue>((TBucket[]) buckets, (TValue[]) values);
        }
    }
}