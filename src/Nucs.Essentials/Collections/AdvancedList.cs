using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Nucs.Collections {

    public class AdvancedList<T> : IList<T>, IReadOnlyList<T>, IList {
        
        public ref T[] InternalArray => ref _arr;
        internal int _count = 0;
        internal T[] _arr;

        
        internal Span<T> AsUnlockedSpan => new Span<T>(_arr, 0, _count);

        
        public int Count {
            get { return _count; }
        }

        
        public int Capacity {
            get { return _arr.Length; }
        }


        
        public AdvancedList(T[] array, int count) {
            _arr = array;
            _count = count;
        }


        public AdvancedList(int initialCapacity) : this(new T[initialCapacity], 0) { }

        public AdvancedList() : this(16) { }

        public AdvancedList(IEnumerable<T> items) {
            _arr = items.ToArray();
            _count = _arr.Length;
        }

        public void Add(T item) {
            var newCount = _count + 1;
            EnsureCapacity(newCount);
            _arr[_count] = item;
            _count = newCount;
        }

        public ref T AddInline(T item) {
            var newCount = _count + 1;
            EnsureCapacity(newCount);
            _arr[_count] = item;
            _count = newCount;
            return ref _arr[_count];
        }

        internal void AddUnlocked(T item) {
            var newCount = _count + 1;
            EnsureCapacity(newCount);
            _arr[_count] = item;
            _count = newCount;
        }

        public void AddRange(IEnumerable<T> items) {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var arr = items as T[] ?? items.ToArray();
            var newCount = _count + arr.Length;
            EnsureCapacity(newCount);
            Array.Copy(arr, 0, _arr, _count, arr.Length);
            _count = newCount;
        }

        public void AddRange(T[] items) {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var newCount = _count + items.Length;
            EnsureCapacity(newCount);
            Array.Copy(items, 0, _arr, _count, items.Length);
            _count = newCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCapacity(int capacity) {
            if (_arr.Length >= capacity)
                return;

            int quadrupled;
            checked {
                try {
                    quadrupled = _arr.Length * 4;
                } catch (OverflowException) {
                    quadrupled = int.MaxValue;
                }
            }

            Array.Resize(ref _arr, Math.Max(quadrupled, capacity));
        }

        public bool Remove(T item) {
            var i = IndexOfInternal(item);

            if (i == -1)
                return false;

            RemoveAtInternal(i);
            return true;
        }

        public IEnumerator<T> GetEnumerator() {
            for (int i = 0; i < _count; i++)
                // deadlocking potential mitigated by lock recursion enforcement
                yield return _arr[i];
        }

        internal IEnumerable<T> GetUnlockedEnumerator() {
            for (int i = 0; i < _count; i++)
                yield return _arr[i];
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public int IndexOf(T item) {
            return IndexOfInternal(item);
        }

        private int IndexOfInternal(T item) {
            return Array.FindIndex(_arr, 0, _count, x => x.Equals(item));
        }

        public void Insert(int index, T item) {
            if (index > _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var newCount = _count + 1;
            EnsureCapacity(newCount);

            // shift everything right by one, starting at index
            Array.Copy(_arr, index, _arr, index + 1, _count - index);

            // insert
            _arr[index] = item;
            _count = newCount;
        }

        public void RemoveAt(int index) {
            if (index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            RemoveAtInternal(index);
        }

        private void RemoveAtInternal(int index) {
            Array.Copy(_arr, index + 1, _arr, index, _count - index - 1);
            _count--;

            // release last element
            Array.Clear(_arr, _count, 1);
        }


        public void Clear() {
            Array.Clear(_arr, 0, _count);
            _count = 0;
        }

        public T[] ToArray() {
            var ret = new T[_count];
            Array.Copy(_arr, ret, _count);
            return ret;
        }

        public bool Contains(T item) {
            return IndexOfInternal(item) != -1;
        }

        public bool Contains(T item, Func<T, T, bool> comparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (comparer(item, ptr)) {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(Func<T, bool> comparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (comparer(ptr)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Changes the count to given number by adding default(T).
        /// </summary>
        /// <param name="count"></param>
        public void ExpandEmpty(int count) {
            EnsureCapacity(count);
            _count = count;
        }

        public bool TryGetValue(out T item, Func<T, bool> selector) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (selector(ptr)) {
                    item = ptr;
                    return true;
                }
            }

            item = default;
            return false;
        }

        /// <summary>
        ///     Tries to add the object.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="comparer">Comparer to identify if this item already exists.</param>
        /// <returns>True if object added</returns>
        public bool TryAdd(T item, Func<T, T, bool> comparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (comparer(ptr, item)) {
                    return false;
                }
            }

            var newCount = _count + 1;
            EnsureCapacity(newCount);
            _arr[_count] = item;
            _count = newCount;

            return true;
        }

        /// <summary>
        ///     Tries to add the object.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="comparer">Comparer to identify if this item already exists.</param>
        /// <returns>True if object added</returns>
        public bool TryAdd(T item, Func<T, bool> comparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (comparer(ptr)) {
                    return false;
                }
            }

            Add(item);
            return true;
        }

        public bool TryAdd(Func<T> factory, Func<T, bool> comparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (comparer(ptr)) {
                    return false;
                }
            }

            Add(factory());
            return true;
        }


        public T Find(Func<T, bool> selector) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (selector(ptr)) {
                    return ptr;
                }
            }

            throw new ArgumentOutOfRangeException("Unable to find item using selector: " + selector);
        }

        public T FindOrDefault(Func<T, bool> selector, T @default = default) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (selector(ptr)) {
                    return ptr;
                }
            }

            return @default;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            if (_count > array.Length - arrayIndex)
                throw new ArgumentException("Destination array was not long enough.");

            Array.Copy(_arr, 0, array, arrayIndex, _count);
        }

        
        public bool IsReadOnly {
            get { return false; }
        }

        
        bool IList.IsFixedSize => false;

        
        T IList<T>.this[int index] {
            get {
                if (index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _arr[index];
            }
            set {
                if (index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                _arr[index] = value;
            }
        }

        
        T IReadOnlyList<T>.this[int index] {
            get {
                if (index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _arr[index];
            }
        }

        
        public ref T this[int index] {
            get {
                if (index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return ref _arr[index];
            }
        }

        public void Trim() {
            if (_count == _arr.Length)
                return;

            Array.Resize(ref _arr, _count);
        }

        public void DoSync(Action<AdvancedList<T>> action) {
            GetSync(l => {
                action(l);
                return 0;
            });
        }

        public TResult GetSync<TResult>(Func<AdvancedList<T>, TResult> func) {
            return func(this);
        }


        #region IList

        void ICollection.CopyTo(Array array, int index) {
            CopyTo((T[]) array, index);
        }


        object ICollection.SyncRoot {
            get { return null; }
        }

        bool ICollection.IsSynchronized {
            get { return true; }
        }

        void IList.Remove(object value) {
            Remove((T) value);
        }

        object IList.this[int index] {
            get { return this[index]; }
            set { this[index] = (T) value; }
        }

        int IList.Add(object value) {
            Add((T) value);
            return 1;
        }

        bool IList.Contains(object value) {
            return Contains((T) value);
        }

        int IList.IndexOf(object value) {
            return IndexOf((T) value);
        }

        void IList.Insert(int index, object value) {
            Insert(index, (T) value);
        }

        #endregion

        #region Implementation of IMessagePackSerializationCallbackReceiver

        public void OnBeforeSerialize() {
            if (Count == Capacity)
                return;

            Array.Resize(ref _arr, Count);
        }

        public void OnAfterDeserialize() { }

        #endregion
    }
}