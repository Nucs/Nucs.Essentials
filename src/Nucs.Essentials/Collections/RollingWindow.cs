using System;
using System.Collections;
using System.Collections.Generic;
using Nucs.Collections.Structs;

//using FluentAssertions;

namespace Nucs.Collections {

    /// <summary>
    ///     A FILO rolling list of N size throwing away the tail while pushing new items to head
    /// </summary>
    public class RollingWindow<T> : IEnumerable<T> {
        /// the backing list object used to hold the data
        public StructList<T> Data;

        /// the most recently removed item from the window (fell off the back)
        private T _mostRecentlyRemoved;

        /// the total number of samples taken by this indicator
        private long _samples;

        /// used to locate the last item in the window as an indexer into the _list
        private int _tail;

        /// <summary>
        ///     The latest datapoint
        /// </summary>
        
        public T Latest {
            get {
                (T[] arr, int count) = Data;
                return arr[(count + _tail - ((int) (Math.Min(_samples, count) - 1)) - 1) % count];
            }
        }

        /// <summary>
        ///     The newest datapoint
        /// </summary>
        
        public T Newest {
            get {
                (T[] arr, int count) = Data;
                return arr[(count + _tail - 1) % count];
            }
        }

        /// <summary>
        ///     Initializes a new instance of the RollwingWindow class with the specified window size.
        /// </summary>
        /// <param name="size">The number of items to hold in the window</param>
        public RollingWindow(int size) {
            if (size < 1) {
                throw new ArgumentException("RollingWindow must have size of at least 1.", nameof(size));
            }

            Data = new StructList<T>(size);
            Size = size;
            _mostRecentlyRemoved = default!;
        }

        
        public RollingWindow(int tail, long samples, T mostRecentlyRemoved, StructList<T> data) {
            Data = data;
            _mostRecentlyRemoved = mostRecentlyRemoved;
            _samples = samples;
            _tail = tail;
        }

        /// <summary>
        ///     Gets the size/capacity of this window
        /// </summary>
        public int Size { get; }

        /// <summary>
        ///     Gets the current number of elements in this window
        /// </summary>
        
        public int Count => Data.Count;

        /// <summary>
        ///     Gets the number of samples that have been added to this window over its lifetime
        /// </summary>
        
        public long Samples => _samples;

        /// <summary>
        ///     Gets the most recently removed item from the window. This is the
        ///     piece of data that just 'fell off' as a result of the most recent
        ///     add. If no items have been removed, this will throw an exception.
        /// </summary>
        
        public T MostRecentlyRemoved {
            get {
                #if DEBUG
                if (Samples <= Size) {
                    throw new InvalidOperationException("No items have been removed yet!");
                }
                #endif

                return _mostRecentlyRemoved;
            }
        }

        /// <summary>
        ///     Indexes into this window, where index 0 is the most recently
        ///     entered value
        /// </summary>
        /// <param name="i">the index, i</param>
        /// <returns>the ith most recent entry</returns>
        public T this[int i] {
            get {
                #if DEBUG
                if (Count == 0) {
                    throw new ArgumentOutOfRangeException(nameof(i), "Rolling window is empty");
                } else if (i > Size - 1 || i < 0) {
                    throw new ArgumentOutOfRangeException(nameof(i), i,
                                                          $"Index must be between 0 and {Size - 1} (rolling window is of size {Size})"
                    );
                } else if (i > Count - 1) {
                    throw new ArgumentOutOfRangeException(nameof(i), i,
                                                          $"Index must be between 0 and {Count - 1} (entry {i} does not exist yet)"
                    );
                }
                #endif
                (T[] arr, int count) = Data;
                return arr[(count + _tail - i - 1) % count];
            }
            set {
                #if DEBUG
                if (i < 0 || i > Count - 1) {
                    throw new ArgumentOutOfRangeException(nameof(i), i, $"Must be between 0 and {Count - 1}");
                }
                #endif
                (T[] arr, int count) = Data;
                arr[(count + _tail - i - 1) % count] = value;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether or not this window is ready, i.e,
        ///     it has been filled to its capacity
        /// </summary>
        
        public bool IsReady => Samples >= Size;

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator() {
            // we make a copy on purpose so the enumerator isn't tied
            // to a mutable object, well it is still mutable but out of scope
            return Data.GetReversedEnumerator(_tail, Count);
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        ///     Adds an item to this window and shifts all other elements
        /// </summary>
        /// <param name="item">The item to be added</param>
        public T Push(T item) {
            _samples++;
            (T[] arr, int count) = Data;
            if (Size == count) {
                // keep track of what's the last element
                // so we can reindex on this[ int ]
                _mostRecentlyRemoved = arr[_tail];
                arr[_tail] = item;
                _tail = (_tail + 1) % Size;
                return _mostRecentlyRemoved;
            } else {
                Data.Add(item);
                return default;
            }
        }

        /// <summary>
        ///     Clears this window of all data
        /// </summary>
        public void Reset() {
            _samples = 0;
            _mostRecentlyRemoved = default;
            _tail = 0;
            Data.Clear();
        }
    }
}