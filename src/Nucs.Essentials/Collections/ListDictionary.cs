using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Nucs.Collections.Layouts;
using Nucs.Collections.Structs;

namespace Nucs.Collections {
    /// <summary>
    ///     A dictionary storing everything in a List&lt;(TKey Key, TValue Value)&gt; in a sorted fashion.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>

    public class ListedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IEnumerable<(TKey Key, TValue Value)> {
        public readonly List<(TKey Key, TValue Value)> Entries;

        public ListedDictionary() {
            Entries = new List<(TKey Key, TValue Value)>();
        }

        public ListedDictionary(Dictionary<TKey, TValue> other) {
            Entries = new List<(TKey Key, TValue Value)>(other.Select(kv => (kv.Key, kv.Value)));
        }

        
        public ListedDictionary(List<(TKey Key, TValue Value)> existing) {
            Entries = existing ?? new List<(TKey Key, TValue Value)>();
        }

        public ListedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> existing) {
            Entries = new List<(TKey Key, TValue Value)>(existing.Select(kv => (kv.Key, kv.Value)));
        }

        public ListedDictionary(int capacity) {
            Entries = new List<(TKey Key, TValue Value)>(capacity);
        }

        public List<(TKey Key, TValue Value)>.Enumerator GetEnumerator() {
            return Entries.GetEnumerator();
        }

        IEnumerator<(TKey Key, TValue Value)> IEnumerable<(TKey Key, TValue Value)>.GetEnumerator() {
            return Entries.GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
            return Entries.Select(kv => new KeyValuePair<TKey, TValue>(kv.Key, kv.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>) this).GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            Entries.Add((item.Key, item.Value));
        }

        public void AddRange(ListedDictionary<TKey, TValue> parentValuesList) {
            Entries.AddRange(parentValuesList);
        }

        public void Clear() {
            Entries.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return Entries.Contains((item.Key, item.Value));
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            var len = Entries.Count;
            for (int i = 0; i < len; i++) {
                var kv = Entries[i];
                array[i + arrayIndex] = new KeyValuePair<TKey, TValue>(kv.Key, kv.Value);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            return Entries.Remove((item.Key, item.Value));
        }

        public int Count => Entries.Count;
        public bool IsReadOnly => false;

        #region Implementation of IDictionary<TKey,TValue>

        public void Add(TKey key, TValue value) {
            Entries.Add((key, value));
        }

        public bool ContainsKey(TKey key) {
            var len = Entries.Count;
            for (int i = 0; i < len; i++) {
                if (KeyComparer.Equals(Entries[i].Key, key)) {
                    return true;
                }
            }

            return false;
        }

        public bool Remove(TKey key) {
            var index = IndexOf(key);
            if (index == -1)
                return false;

            Entries.RemoveAt(index);
            return true;
        }

        public int IndexOf(TKey key) {
            var len = Entries.Count;
            for (int i = 0; i < len; i++) {
                if (KeyComparer.Equals(Entries[i].Key, key)) {
                    return i;
                }
            }

            return -1;
        }

        public bool TryGetValue(TKey key, out TValue value) {
            var len = Entries.Count;
            for (int i = 0; i < len; i++) {
                if (KeyComparer.Equals(Entries[i].Key, key)) {
                    value = Entries[i].Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public TValue this[TKey key] {
            get {
                var len = Entries.Count;
                for (int i = 0; i < len; i++) {
                    if (KeyComparer.Equals(Entries[i].Key, key)) {
                        return Entries[i].Value;
                    }
                }

                throw new KeyNotFoundException(key?.ToString());
            }
            set {
                var len = Entries.Count;
                for (int i = 0; i < len; i++) {
                    if (KeyComparer.Equals(Entries[i].Key, key)) {
                        Entries[i] = (key, value);
                        return;
                    }
                }

                Entries.Add((key, value));
            }
        }

        /// <summary>
        ///     Returns an unsafe reference to the location of the item in the list. 
        /// </summary>
        /// <remarks>Not thread-safe.</remarks>
        public ref (TKey Key, TValue Value) this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                var entires = Entries;
                return ref Unsafe.As<List<(TKey, TValue)>, ListLayout<(TKey, TValue)>>(ref entires)._items[index];
            }
        }

        /// <summary>
        ///     Returns unsafe reference to the location of the internal array. If the <see cref="Entries"/> were to expand
        ///     due to capacity change then this array would stop getting updates. Therefore use with care and dont hold
        ///     a reference to it for long if at all.
        /// </summary>
        /// <remarks>Use to perform updates with ref capabilities</remarks>
        public StructList<(TKey Key, TValue Value)> InternalArray {
            get {
                var entires = Entries;
                return new StructList<(TKey Key, TValue Value)>(Unsafe.As<List<(TKey, TValue)>, ListLayout<(TKey, TValue)>>(ref entires)._items, Count);
            }
        }

        public EqualityComparer<TKey> KeyComparer = EqualityComparer<TKey>.Default;

        public ICollection<TKey> Keys => KeysIterator.ToArray();
        public ICollection<TValue> Values => ValuesIterator.ToArray();

        public Enumerable<KeyEnumerator, TKey> KeysIterator => new Enumerable<KeyEnumerator, TKey>(new KeyEnumerator(InternalArray._arr, 0, Count));
        public Enumerable<ValueEnumerator, TValue> ValuesIterator => new Enumerable<ValueEnumerator, TValue>(new ValueEnumerator(InternalArray._arr, 0, Count));

        #endregion

        public void SortBy(Comparison<(TKey Key, TValue Value)> comparer) {
            this.Entries.Sort(comparer);
        }

        public struct KeyEnumerator : IEnumerator<TKey> {
            private readonly (TKey, TValue)[] _array;
            private readonly int _start;
            private readonly int _end; // cache Offset + Count, since it's a little slow
            private int _current;

            public KeyEnumerator((TKey, TValue)[] array, int start, int end) {
                _array = array;
                _start = start;
                _end = start + end;
                _current = start - 1;
            }

            public bool MoveNext() {
                if (_current < _end) {
                    _current++;
                    return _current < _end;
                }

                return false;
            }

            public readonly TKey Current {
                get {
                    #if DEBUG
                    if (_current < _start)
                        throw new InvalidOperationException("ThrowInvalidOperationException_InvalidOperation_EnumNotStarted");
                    if (_current >= _end)
                        throw new InvalidOperationException("ThrowInvalidOperationException_InvalidOperation_EnumEnded");
                    #endif
                    return _array![_current].Item1;
                }
            }

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                _current = _start - 1;
            }

            public void Dispose() { }
        }

        public struct ValueEnumerator : IEnumerator<TValue> {
            private readonly (TKey, TValue)[] _array;
            private readonly int _start;
            private readonly int _end; // cache Offset + Count, since it's a little slow
            private int _current;

            public ValueEnumerator((TKey, TValue)[] array, int start, int end) {
                _array = array;
                _start = start;
                _end = start + end;
                _current = start - 1;
            }

            public bool MoveNext() {
                if (_current < _end) {
                    _current++;
                    return _current < _end;
                }

                return false;
            }

            public readonly TValue Current {
                get {
                    #if DEBUG
                    if (_current < _start)
                        throw new InvalidOperationException("ThrowInvalidOperationException_InvalidOperation_EnumNotStarted");
                    if (_current >= _end)
                        throw new InvalidOperationException("ThrowInvalidOperationException_InvalidOperation_EnumEnded");
                    #endif
                    return _array![_current].Item2;
                }
            }

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                _current = _start - 1;
            }

            public void Dispose() { }
        }
    }
}