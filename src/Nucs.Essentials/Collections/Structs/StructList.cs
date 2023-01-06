using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nucs.Collections.Layouts;
using Nucs.Exceptions;

namespace Nucs.Collections.Structs {
    internal ref struct StructListDebugView<T> {
        public StructListDebugView(StructList<T> collection) : this() {
            if (collection.IsNullOrEmpty)
                return;

            Array = collection.AsSpan();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ReadOnlySpan<T> Array;
    }

    [DebuggerTypeProxy(typeof(StructListDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    public struct StructList<T> : IList<T>, IReadOnlyList<T>, /*IStructEnumerable<T, IListEnumerator<T, StructList<T>>>,*/ IList, IDisposable {
        internal int _count;

        internal T[] _arr;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        public readonly bool IsNull => _arr == null;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        public readonly bool IsNullOrEmpty {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _arr == null || _count == 0; }
        }

        internal readonly Span<T> AsUnlockedSpan => new Span<T>(_arr, 0, _count);

        public readonly T[] InternalArray {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _arr; }
        }

        public readonly int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }


        public readonly int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }


        public readonly int Capacity {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _arr.Length; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out T[] arr, out int count) {
            arr = _arr;
            count = _count;
        }

        public StructList(T[] array, int count) {
            _arr = array;
            _count = count;
        }

        public StructList(T[] items) {
            _arr = items;
            _count = items.Length;
        }

        public StructList(int initialCapacity) {
            _arr = initialCapacity == 0 ? Array.Empty<T>() : new T[initialCapacity];
            _count = 0;
        }

        public StructList(IEnumerable<T> items) {
            _arr = items.ToArray();
            _count = _arr.Length;
        }

        public StructList(ICollection<T> items) {
            _arr = items.ToArray();
            _count = _arr.Length;
        }

        public StructQueue<T> AsQueue(int startIndex = 0) {
            return new StructQueue<T>(_arr, startIndex, _count);
        }

        public ReusableStructListQueue<T> AsReusableQueue(int startIndex = 0) {
            return new ReusableStructListQueue<T>(ref this, startIndex, _count);
        }

        public void Add(T item) {
            EnsureCapacity(++_count);
            _arr[_count - 1] = item;
        }

        public void Add(ref T item) {
            EnsureCapacity(++_count);
            _arr[_count - 1] = item;
        }

        public void AddRange(Span<T> items) {
            var initialSize = _count;
            EnsureCapacity(initialSize + items.Length);
            _count += items.Length;
            items.CopyTo(new Span<T>(_arr, initialSize, _count - initialSize));
        }

        public void AddRange(ReadOnlySpan<T> items) {
            var initialSize = _count;
            EnsureCapacity(initialSize + items.Length);
            _count += items.Length;
            items.CopyTo(new Span<T>(_arr, initialSize, _count - initialSize));
        }

        public void AddRange(ReadOnlyMemory<T> items) {
            var initialSize = _count;
            EnsureCapacity(initialSize + items.Length);
            _count += items.Length;
            items.CopyTo(_arr.AsMemory(initialSize));
        }

        public void AddRange(ArraySegment<T> items) {
            var initialSize = _count;
            EnsureCapacity(initialSize + items.Count);
            _count += items.Count;
            items.CopyTo(new ArraySegment<T>(_arr, initialSize, _count - initialSize));
        }

        public int AddToEnd(T item) {
            var idx = _count++;
            EnsureCapacity(idx + 1);
            _arr[idx] = item;
            return idx;
        }

        public int AddToEnd(ref T item) {
            var idx = _count++;
            EnsureCapacity(idx + 1);
            _arr[idx] = item;
            return idx;
        }

        public T AddInline(T item) {
            var newCount = _count + 1;
            EnsureCapacity(newCount);
            _arr[_count] = item;
            _count = newCount;
            return item;
        }

        public ref T AddRefInline(T item) {
            var newCount = _count + 1;
            EnsureCapacity(newCount);
            ref var itemAt = ref _arr[_count];
            itemAt = item;
            _count = newCount;
            return ref itemAt;
        }

        internal void AddUnlocked(T item) {
            var newCount = _count + 1;
            EnsureCapacity(newCount);
            _arr[_count] = item;
            _count = newCount;
        }

        public T AddInline(ref T item) {
            var newCount = _count + 1;
            EnsureCapacity(newCount);
            _arr[_count] = item;
            _count = newCount;
            return item;
        }

        internal void AddUnlocked(ref T item) {
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

        public void AddRange(StructList<T> items) {
            if (items.IsNull)
                throw new ArgumentNullException(nameof(items));

            var newCount = _count + items.Count;
            EnsureCapacity(newCount);
            Array.Copy(items._arr, 0, _arr, _count, items.Count);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void EnsureCapacity(int capacity) {
            if (_arr.Length >= capacity)
                return;

            int quadrupled;
            checked {
                try {
                    quadrupled = _arr.Length * 4;
                } catch (OverflowException) {
                    if (_arr.Length == int.MaxValue)
                        throw;
                    quadrupled = int.MaxValue;
                }
            }

            Array.Resize(ref _arr, Math.Max(quadrupled, capacity));
        }

        public void OverrideCount(int newCount) {
            this.EnsureCapacity(newCount);
            _count = newCount;
        }

        public bool Remove(T item) {
            var i = IndexOfInternal(item);

            if (i == -1)
                return false;

            RemoveAtInternal(i);
            return true;
        }

        public bool Remove(T item, Func<T, T, bool> comparison) {
            var i = IndexOfInternal(item);

            if (i == -1)
                return false;

            RemoveAtInternal(i);
            return true;
        }

        public int RemoveAll(PredicateByRef<T> comparison) {
            if (_count == 0 || _arr is null)
                return 0;
            int removed = 0;
            var array = _arr;
            for (int i = _count - 1; i >= 0; i--) {
                if (comparison(ref array[i])) {
                    removed++;
                    RemoveAtInternal(i);
                }
            }

            return removed;
        }

        public int RemoveAll<TState>(ref TState state, StatePredicateHandler<TState, T> comparison) {
            if (_count == 0 || _arr is null)
                return 0;
            int removed = 0;
            var array = _arr;
            for (int i = _count - 1; i >= 0; i--) {
                if (comparison(ref state, ref array[i])) {
                    removed++;
                    RemoveAtInternal(i);
                }
            }

            return removed;
        }

        public int RemoveAll<TState>(TState state, StatePredicateHandler<TState, T> comparison) {
            if (_count == 0 || _arr is null)
                return 0;
            int removed = 0;
            var array = _arr;
            for (int i = _count - 1; i >= 0; i--) {
                if (comparison(ref state, ref array[i])) {
                    removed++;
                    RemoveAtInternal(i);
                }
            }

            return removed;
        }

        /// <summary>
        ///     Removes <paramref name="length"/> items from the start of the list.
        /// </summary>
        /// <param name="length">Count of items to remove</param>
        public void RemoveStart(int length) {
            var originalCount = _count;
            length = Math.Min(length, originalCount);
            _arr.AsSpan(length, originalCount - length)
                .CopyTo(new Span<T>(_arr));
            _count -= length;
        }

        /// <summary>
        ///     Removes <paramref name="length"/> items from the end of the list.
        /// </summary>
        /// <param name="length">Count of items to remove</param>
        public void RemoveEnd(int length) {
            var originalCount = _count;
            _count -= Math.Min(length, originalCount);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _arr.AsSpan(_count, originalCount).Clear();
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return new Enumerator(_arr, 0, _count);
        }

        public readonly Enumerator GetEnumerator() {
            return new Enumerator(_arr, 0, _count);
        }

        public readonly Enumerator GetEnumerator(int start, int count) {
            return new Enumerator(_arr, start, _count);
        }

        public readonly IEnumerator<T> GetReversedEnumerator(int start, int count) {
            for (int i = count - 1; i >= start; i--)
                // deadlocking potential mitigated by lock recursion enforcement
                yield return _arr[i];
        }

        internal readonly IEnumerable<T> GetUnlockedEnumerator() {
            for (int i = 0; i < _count; i++)
                yield return _arr[i];
        }

        readonly IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public readonly int IndexOf(T item) {
            return IndexOfInternal(item);
        }

        private readonly int IndexOfInternal(T item, Func<T, T, bool> comparer) {
            var len = _count;
            var arr = _arr;
            if (item is object) {
                for (int i = 0; i < len; i++) {
                    if (comparer(item, arr[i]))
                        return i;
                }
            } else {
                for (int i = 0; i < len; i++) {
                    if (comparer.Equals(arr[i]))
                        return i;
                }
            }

            return -1;
        }

        private readonly int IndexOfInternal(T item) {
            var len = _count;
            var arr = _arr;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                for (int i = 0; i < len; i++) {
                    if (Equals(item, arr[i]))
                        return i;
                }
            } else {
                for (int i = 0; i < len; i++) {
                    if (item.Equals(arr[i]))
                        return i;
                }
            }

            return -1;
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

        /// <summary>Inserts the elements of a collection into the <see cref="T:System.Collections.Generic.List`1" /> at the specified index.</summary>
        /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="collection">The collection whose elements should be inserted into the <see cref="T:System.Collections.Generic.List`1" />. The collection itself cannot be <see langword="null" />, but it can contain elements that are <see langword="null" />, if type <paramref name="T" /> is a reference type.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="collection" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///         <paramref name="index" /> is less than 0.
        /// -or-
        /// <paramref name="index" /> is greater than <see cref="P:System.Collections.Generic.List`1.Count" />.</exception>
        public void InsertRange(int index, IEnumerable<T> collection) {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if ((uint) index > (uint) this._count) throw new ArgumentOutOfRangeException(nameof(index));
            if (collection is IList<T> objs) {
                int count = objs.Count;
                if (count > 0) {
                    this.EnsureCapacity(_arr.Length + count);
                    if (index < this._count)
                        Array.Copy(this._arr, index, this._arr, index + count, this._count - index);

                    if (this._arr == objs) {
                        // Copy first part of _arr to insert location
                        Array.Copy(_arr, 0, _arr, index, index);
                        // Copy last part of _arr back to inserted location
                        Array.Copy(_arr, index + count, _arr, index * 2, this._count - index);
                    } else {
                        objs.CopyTo(_arr, index);
                    }

                    this._count += count;
                }
            } else {
                foreach (T obj in collection)
                    this.Insert(index++, obj);
            }
        }


        public void InsertRange(int index, IList<T> objs) {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            if ((uint) index > (uint) _count) throw new ArgumentOutOfRangeException(nameof(index));
            int count = objs.Count;
            if (count > 0) {
                EnsureCapacity(_count + count);
                if (index < _count)
                    Array.Copy(_arr, index, _arr, index + count, _count - index);

                if (_arr == objs) {
                    // Copy first part of _arr to insert location
                    Array.Copy(_arr, 0, _arr, index, index);
                    // Copy last part of _arr back to inserted location
                    Array.Copy(_arr, index + count, _arr, index * 2, _count - index);
                } else {
                    objs.CopyTo(_arr, index);
                }

                _count += count;
            }
        }

        public void InsertRange(int index, Span<T> objs) {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            if ((uint) index > (uint) _count) throw new ArgumentOutOfRangeException(nameof(index));
            int count = objs.Length;
            if (count > 0) {
                EnsureCapacity(_count + count);
                if (index < _count)
                    Array.Copy(_arr, index, _arr, index + count, _count - index);

                if (_arr == objs) {
                    // Copy first part of _arr to insert location
                    Array.Copy(_arr, 0, _arr, index, index);
                    // Copy last part of _arr back to inserted location
                    Array.Copy(_arr, index + count, _arr, index * 2, _count - index);
                } else {
                    objs.CopyTo(_arr.AsSpan(index));
                }

                _count += count;
            }
        }

        public void InsertRange(int index, StructList<T> objs) {
            if (objs.IsNull) throw new ArgumentNullException(nameof(objs));
            if ((uint) index > (uint) _count) throw new ArgumentOutOfRangeException(nameof(index));
            int count = objs.Count;
            if (count > 0) {
                EnsureCapacity(_count + count);
                if (index < _count)
                    Array.Copy(_arr, index, _arr, index + count, _count - index);

                if (_arr == objs.InternalArray) {
                    // Copy first part of _arr to insert location
                    Array.Copy(_arr, 0, _arr, index, index);
                    // Copy last part of _arr back to inserted location
                    Array.Copy(_arr, index + count, _arr, index * 2, _count - index);
                } else {
                    objs.CopyTo(_arr, index);
                }

                _count += count;
            }
        }

        public void RemoveAt(int index) {
            if (index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            RemoveAtInternal(index);
        }

        private void RemoveAtInternal(int index) {
            _count--;

            if (index == _count) {
                //pop last
                _arr[index] = default(T)!;
                return;
            }

            //copy all items past index ontop of removed index
            Array.Copy(_arr, index + 1, _arr, index, _count - index);

            // release last element
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _arr[_count] = default!;
        }

        public void RemoveAt(int index, out T value) {
            if (index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            RemoveAtInternal(index, out value);
        }

        private void RemoveAtInternal(int index, out T value) {
            _count--;

            value = _arr[index];
            if (index == _count) {
                //pop last
                _arr[index] = default(T)!;
                return;
            }

            //copy all items past index ontop of removed index
            Array.Copy(_arr, index + 1, _arr, index, _count - index);

            // release last element
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _arr[_count] = default!;
        }

        public void Distinct() {
            var len = _count;
            for (int i = len - 1; i >= 0; i--) {
                for (int j = 0; j < len; j++) {
                    if (i == j)
                        continue;
                    if (Equals(_arr[i], _arr[j])) {
                        RemoveAtInternal(i);
                        len--;
                        break;
                    }
                }
            }
        }

        public void Distinct(CompareByRef<T> comparer) {
            var len = _count;
            for (int i = len - 1; i >= 0; i--) {
                for (int j = 0; j < len; j++) {
                    if (i == j)
                        continue;
                    if (comparer(ref _arr[i], ref _arr[j])) {
                        RemoveAtInternal(i);
                        len--;
                        break;
                    }
                }
            }
        }

        public void Clear() {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var cnt = _count;
            if (cnt == 0)
                return;

            var arr = _arr;
            if (arr != null)
                Array.Clear(arr, 0, _count);
            _count = 0;
        }

        public readonly T[] ToArray() {
            var ret = new T[_count];
            if (_count > 0)
                Array.Copy(_arr, ret, _count);
            return ret;
        }

        public readonly List<T> AsList() {
            var ret = new List<T>(0);
            if (IsNullOrEmpty)
                return ret;

            var layout = ret.AsLayout();
            layout._items = _arr;
            layout.Size = _count;

            return ret;
        }

        public readonly List<T> ToList() {
            var ret = new List<T>(_count);
            if (IsNullOrEmpty)
                return ret;

            var layout = ret.AsLayout();
            layout.Size = ret.Capacity;
            _arr.CopyTo(layout._items, 0);

            return ret;
        }

        public readonly List<T> ToList(int capacity) {
            var ret = new List<T>(Math.Max(capacity, _count));
            if (IsNullOrEmpty)
                return ret;

            var layout = ret.AsLayout();
            layout.Size = ret.Capacity;
            _arr.AsSpan(_count).CopyTo(layout._items);

            return ret;
        }

        public readonly T2[] ToArray<T2>() {
            var ret = new T2[_count];
            if (_count > 0)
                Array.Copy(_arr, ret, _count);
            return ret;
        }

        public readonly bool Contains(T item) {
            return IndexOfInternal(item) != -1;
        }

        public readonly bool Contains(T item, Func<T, T, bool> comparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (comparer(item, ptr)) {
                    return true;
                }
            }

            return false;
        }

        public readonly bool Contains(Func<T, bool> comparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (comparer(ptr)) {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(T item, IEqualityComparer<T> equalityComparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (equalityComparer.Equals(item, ptr)) {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(T item, EqualityComparer<T> equalityComparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (equalityComparer.Equals(item, ptr)) {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(ref T item, EqualityComparer<T> equalityComparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (equalityComparer.Equals(item, ptr)) {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(ref T item, IEqualityComparer<T> equalityComparer) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (equalityComparer.Equals(item, ptr)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Changes the count to given number by adding default(T)
        /// </summary>
        /// <param name="count"></param>
        public void ExpandEmpty(int count) {
            EnsureCapacity(count);
            _count = count;
        }

        public readonly bool TryGetValue(out T item, Func<T, bool> selector) {
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


        public readonly T Find(Func<T, bool> selector) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (selector(ptr)) {
                    return ptr;
                }
            }

            throw new ArgumentOutOfRangeException("Unable to find item using selector: " + selector);
        }

        public readonly T Find<TState>(TState state, StatePredicateHandler<TState, T> selector) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (selector(ref state, ref ptr)) {
                    return ptr;
                }
            }

            throw new ArgumentOutOfRangeException("Unable to find item using selector: " + selector);
        }

        public readonly T? FindOrDefault(Func<T, bool> selector, T @default = default) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (selector(ptr)) {
                    return ptr;
                }
            }

            return @default;
        }

        public readonly long IndexOf(Func<T, bool> selector) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (selector(ptr)) {
                    return i;
                }
            }

            return -1;
        }

        public readonly T? FindOrDefault<TState>(TState state, Func<TState, T, bool> selector, T? @default = default) {
            for (long i = 0; i < _count; i++) {
                ref var ptr = ref _arr[i];
                if (selector(state, ptr)) {
                    return ptr;
                }
            }

            return @default;
        }

        public readonly void CopyTo(T[] array, int arrayIndex) {
            if (_count > array.Length - arrayIndex)
                throw new ArgumentException("Destination array was not long enough.");

            Array.Copy(_arr, 0, array, arrayIndex, _count);
        }

        readonly bool IList.IsReadOnly => false;
        readonly bool ICollection<T>.IsReadOnly => false;


        readonly bool IList.IsFixedSize => false;


        readonly T IList<T>.this[int index] {
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


        readonly T IReadOnlyList<T>.this[int index] {
            get {
                if (index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _arr[index];
            }
        }


        public readonly ref T this[int index] {
            get {
                if (index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return ref _arr[index];
            }
        }


        public readonly ref T this[uint index] {
            get {
                if (index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return ref _arr[index];
            }
        }


        public readonly ref T this[long index] {
            get {
                if (index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return ref _arr[index];
            }
        }


        public readonly ref T this[ulong index] {
            get {
                if (index >= (ulong) _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return ref _arr[index];
            }
        }


        public readonly ref T this[byte index] {
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
        
        #if NET6_0_OR_GREATER
        
        /// <summary>
        /// Sorts the elements in the entire <see cref="Span{T}" /> using the <see cref="IComparable{T}" /> implementation
        /// of each element of the <see cref= "Span{T}" />
        /// </summary>
        /// <typeparam name="T">The type of the elements of the span.</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to sort.</param>
        /// <exception cref="InvalidOperationException">
        /// One or more elements in <paramref name="span"/> do not implement the <see cref="IComparable{T}" /> interface.
        /// </exception>
        public void Sort() {
            MemoryExtensions.Sort(AsSpan(), Comparer<T>.Default);
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="Span{T}" /> using the <typeparamref name="TComparer" />.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the span.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer to use to compare elements.</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to sort.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing elements, or null to
        /// use the <see cref="IComparable{T}"/> interface implementation of each element.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, and one or more elements in <paramref name="span"/> do not
        /// implement the <see cref="IComparable{T}" /> interface.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The implementation of <paramref name="comparer"/> caused an error during the sort.
        /// </exception>
        public void Sort<TComparer>(TComparer comparer) where TComparer : IComparer<T>? {
            MemoryExtensions.Sort(AsSpan(), comparer); // value-type comparer will be boxed
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="Span{T}" /> using the specified <see cref="Comparison{T}" />.
        /// </summary>
        /// <typeparam name="T">The type of the elements of the span.</typeparam>
        /// <param name="span">The <see cref="Span{T}"/> to sort.</param>
        /// <param name="comparison">The <see cref="Comparison{T}"/> to use when comparing elements.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
        public void Sort(Comparison<T> comparison) {
            MemoryExtensions.Sort(AsSpan(), comparison);
        }

        /// <summary>
        /// Sorts a pair of spans (one containing the keys and the other containing the corresponding items)
        /// based on the keys in the first <see cref="Span{TKey}" /> using the <see cref="IComparable{T}" />
        /// implementation of each key.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements of the key span.</typeparam>
        /// <typeparam name="TValue">The type of the elements of the items span.</typeparam>
        /// <param name="keys">The span that contains the keys to sort.</param>
        /// <param name="items">The span that contains the items that correspond to the keys in <paramref name="keys"/>.</param>
        /// <exception cref="ArgumentException">
        /// The length of <paramref name="keys"/> isn't equal to the length of <paramref name="items"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// One or more elements in <paramref name="keys"/> do not implement the <see cref="IComparable{T}" /> interface.
        /// </exception>
        public void Sort<TKey>(Span<TKey> keys) =>
            MemoryExtensions.Sort(keys, AsSpan(), (IComparer<TKey>?) null);

        /// <summary>
        /// Sorts a pair of spans (one containing the keys and the other containing the corresponding items)
        /// based on the keys in the first <see cref="Span{TKey}" /> using the specified comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements of the key span.</typeparam>
        /// <typeparam name="TComparer">The type of the comparer to use to compare elements.</typeparam>
        /// <param name="keys">The span that contains the keys to sort.</param>
        /// <param name="items">The span that contains the items that correspond to the keys in <paramref name="keys"/>.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing elements, or null to
        /// use the <see cref="IComparable{T}"/> interface implementation of each element.
        /// </param>
        /// <exception cref="ArgumentException">
        /// The length of <paramref name="keys"/> isn't equal to the length of <paramref name="items"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, and one or more elements in <paramref name="keys"/> do not
        /// implement the <see cref="IComparable{T}" /> interface.
        /// </exception>
        public void Sort<TKey, TComparer>(Span<TKey> keys, TComparer comparer) where TComparer : IComparer<TKey>? {
            MemoryExtensions.Sort(keys, AsSpan(), comparer);
        }

        /// <summary>
        /// Sorts a pair of spans (one containing the keys and the other containing the corresponding items)
        /// based on the keys in the first <see cref="Span{TKey}" /> using the specified comparison.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements of the key span.</typeparam>
        /// <param name="keys">The span that contains the keys to sort.</param>
        /// <param name="items">The span that contains the items that correspond to the keys in <paramref name="keys"/>.</param>
        /// <param name="comparison">The <see cref="Comparison{T}"/> to use when comparing elements.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// The length of <paramref name="keys"/> isn't equal to the length of <paramref name="items"/>.
        /// </exception>
        public void Sort<TKey>(Span<TKey> keys, Comparison<TKey> comparison) {
            MemoryExtensions.Sort(keys, AsSpan(), new ComparisonComparer<TKey>(comparison));
        }

        #endif

        /// <summary>
        /// Searches an entire sorted <see cref="Span{T}"/> for the specified <paramref name="value"/>
        /// using the specified <typeparamref name="TComparer"/> generic type.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <param name="span">The sorted <see cref="Span{T}"/> to search.</param>
        /// <param name="value">The object to locate. The value can be null for reference types.</param>
        /// /// <returns>
        /// The zero-based index of <paramref name="value"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="value"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="value"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="Span{T}.Length"/>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparer" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch<TOrderBy>(TOrderBy value, OrderByDelegate<T, TOrderBy> orderBySelector) where TOrderBy : IComparable<TOrderBy> {
            return SpanHelper.BinarySearch<T, TOrderBy>(ref MemoryMarshal.GetReference(ReadOnlySpan), Length, value, orderBySelector);
        }

        /// <summary>
        /// Searches an entire sorted <see cref="Span{T}"/> for the specified <paramref name="value"/>
        /// using the specified <typeparamref name="TComparer"/> generic type.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <typeparam name="TComparer">The specific type of <see cref="IComparer{T}"/>.</typeparam>
        /// <param name="span">The sorted <see cref="Span{T}"/> to search.</param>
        /// <param name="value">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">The <typeparamref name="TComparer"/> to use when comparing.</param>
        /// /// <returns>
        /// The zero-based index of <paramref name="value"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="value"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="value"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="Span{T}.Length"/>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparer" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch<TOrderBy, TComparer>(TOrderBy value, TComparer comparer, OrderByDelegate<T, TOrderBy> orderBySelector)
            where TComparer : IComparer<TOrderBy>
            where TOrderBy : IComparable<TOrderBy> {
            return SpanHelper.BinarySearch<T, TOrderBy, TComparer>(ref MemoryMarshal.GetReference(ReadOnlySpan), Length, value, orderBySelector, comparer);
        }

        /// <summary>
        /// Searches an entire sorted <see cref="Span{T}"/> for the specified <paramref name="value"/>
        /// using the specified <typeparamref name="TComparer"/> generic type.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <typeparam name="TComparer">The specific type of <see cref="IComparer{T}"/>.</typeparam>
        /// <param name="span">The sorted <see cref="Span{T}"/> to search.</param>
        /// <param name="value">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">The <typeparamref name="TComparer"/> to use when comparing.</param>
        /// /// <returns>
        /// The zero-based index of <paramref name="value"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="value"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="value"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="Span{T}.Length"/>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparer" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch<TComparer>(T value, TComparer comparer) where TComparer : IComparer<T> {
            return MemoryExtensions.BinarySearch(ReadOnlySpan, value, comparer);
        }

        /// <summary>
        /// Searches an entire sorted <see cref="ReadOnlySpan{T}"/> for a value
        /// using the specified <typeparamref name="TComparable"/> generic type.
        /// </summary>
        /// <typeparam name="T">The element type of the span.</typeparam>
        /// <typeparam name="TComparable">The specific type of <see cref="IComparable{T}"/>.</typeparam>
        /// <param name="span">The sorted <see cref="ReadOnlySpan{T}"/> to search.</param>
        /// <param name="comparable">The <typeparamref name="TComparable"/> to use when comparing.</param>
        /// <returns>
        /// The zero-based index of <paramref name="comparable"/> in the sorted <paramref name="span"/>,
        /// if <paramref name="comparable"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="comparable"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="ReadOnlySpan{T}.Length"/>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name = "comparable" /> is <see langword="null"/> .
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BinarySearch<TComparable>(TComparable comparable) where TComparable : IComparable<T> {
            return MemoryExtensions.BinarySearch<T, TComparable>(ReadOnlySpan, comparable);
        }

        public class ComparisonComparer<V> : IComparer<V> {
            private readonly Comparison<V> Comparison;

            public ComparisonComparer(Comparison<V> comparison) =>
                this.Comparison = comparison;

            public int Compare(V x, V y) =>
                this.Comparison(x, y);
        }

        public readonly int Sum(Func<T, int> valueGetter) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int summary = 0;
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                summary += valueGetter(arr[i]);
            }

            return summary;
        }

        public readonly int CountWhere(Func<T, bool> valueGetter) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int summary = 0;
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (valueGetter(arr[i])) summary++;
            }

            return summary;
        }

        public readonly double Sum(Func<T, double> valueGetter) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            double summary = 0;
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                summary += valueGetter(arr[i]);
            }

            return summary;
        }

        public readonly long SumLong(Func<T, long> valueGetter) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            long summary = 0;
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                summary += valueGetter(arr[i]);
            }

            return summary;
        }

        public readonly double Average(Func<T, int> valueGetter) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            long summary = 0;
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                summary += valueGetter(arr[i]);
            }

            return summary / ((double) length);
        }

        public readonly double Average(Func<T, double> valueGetter) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            double summary = 0;
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                summary += valueGetter(arr[i]);
            }

            return summary / length;
        }

        public readonly double Average(Func<T, long> valueGetter) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            long summary = 0;
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                summary += valueGetter(arr[i]);
            }

            return summary / ((double) length);
        }

        public readonly double Std(Func<T, int> valueGetter) {
            T[]? arr = _arr;
            if (arr is null)
                throw new InvalidOperationException("StructList is uninitialized.");
            long summary = 0;
            int length = _count;
            for (int i = 0; i < length; i++) {
                summary += valueGetter(arr[i]);
            }

            double average = summary / ((double) length);

            double std = 0;
            for (int i = 0; i < length; i++) {
                std += Math.Pow(valueGetter(arr[i]) - average, 2);
            }

            return Math.Sqrt((std) / ((double) length - 1));
        }

        public readonly double Std(Func<T, double> valueGetter) {
            T[]? arr = _arr;
            if (arr is null)
                throw new InvalidOperationException("StructList is uninitialized.");
            double summary = 0;
            int length = _count;
            for (int i = 0; i < length; i++) {
                summary += valueGetter(arr[i]);
            }

            double average = summary / length;

            summary = 0;
            for (int i = 0; i < length; i++) {
                summary += Math.Pow(valueGetter(arr[i]) - average, 2);
            }

            return Math.Sqrt((summary) / ((double) length - 1));
        }

        public readonly double Std(Func<T, long> valueGetter) {
            T[]? arr = _arr;
            if (arr is null)
                throw new InvalidOperationException("StructList is uninitialized.");
            long summary = 0;
            int length = _count;
            for (int i = 0; i < length; i++) {
                summary += valueGetter(arr[i]);
            }

            double average = summary / ((double) length);

            double std = 0;
            for (int i = 0; i < length; i++) {
                std += Math.Pow(valueGetter(arr[i]) - average, 2);
            }

            return Math.Sqrt((std) / ((double) length - 1));
        }

        public readonly T[] Where(Predicate<T> predicate, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<T> list = new StructList<T>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(item)) {
                    list.Add(ref item);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelect<TRet>(Predicate<T> predicate, Func<T, TRet> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(item)) {
                    list.Add(select(item));
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelect<TRet>(Predicate<T> predicate, SelectHandler<T, TRet> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(item)) {
                    list.Add(select(ref item));
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelect<TState, TRet>(TState state, Predicate<TState, T> predicate, SelectHandler<TState, T, TRet> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(state, item)) {
                    list.Add(select(ref state, ref item));
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelect<TRet>(Predicate<T> predicate, SelectHandlerByRef<T, TRet> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(item)) {
                    list.Add(select(ref item));
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelect<TRet>(PredicateByRef<T> predicate, Func<T, TRet> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref item)) {
                    list.Add(select(item));
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelectMany<TRet>(PredicateByRef<T> predicate, Func<T, TRet[]> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref item)) {
                    var arr2 = select(item);
                    var length2 = arr2.Length;
                    for (int j = 0; j < length2; j++) {
                        list.Add(arr2[j]);
                    }
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelectMany<TRet>(PredicateByRef<T> predicate, SelectManyWhereHandler<T, TRet> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref item)) {
                    var arr2 = select(ref item);
                    var length2 = arr2.Length;
                    for (int j = 0; j < length2; j++) {
                        list.Add(arr2[j]);
                    }
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelectMany<TState, TRet>(TState state, PredicateByRef<TState, T> predicate, SelectManyWhereHandler<TState, T, TRet> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref state, ref item)) {
                    var arr2 = select(ref state, ref item);
                    var length2 = arr2.Length;
                    for (int j = 0; j < length2; j++) {
                        list.Add(arr2[j]);
                    }
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelectMany<TRet>(PredicateByRef<T> predicate, Func<T, IEnumerable<TRet>> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref item)) {
                    foreach (var ret in @select(item)) {
                        list.Add(ret);
                    }
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelect<TRet>(PredicateByRef<T> predicate, SelectHandler<T, TRet> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref item)) {
                    list.Add(select(ref item));
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] WhereSelect<TRet>(PredicateByRef<T> predicate, SelectHandlerByRef<T, TRet> select, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref item)) {
                    list.Add(select(ref item));
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] Select<TRet>(Func<T, TRet> predicate, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                list.Add(predicate(item));
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] Select<TRet>(SelectHandler<T, TRet> predicate, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                list.Add(predicate(ref item));
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] Select<TRet>(SelectHandlerByRef<T, TRet> predicate, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                list.Add(predicate(ref item));
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] Select<TState, TRet>(TState state, SelectHandlerByRef<TState, T, TRet> predicate, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                list.Add(ref predicate(ref state, ref item));
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TRet>(SelectManyWhereHandler<T, TRet> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                var arr2 = select(ref item);
                var length2 = arr2.Length;
                for (int j = 0; j < length2; j++) {
                    list.Add(arr2[j]);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TRet>(SelectManyWhereHandler<T, TRet[]> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                var arr2 = select(ref item);
                var length2 = arr2.Length;
                for (int j = 0; j < length2; j++) {
                    list.AddRange(arr2[j]);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TRet>(SelectManyWhereHandler<T, StructList<TRet>> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                var arr2 = select(ref item);
                var length2 = arr2.Length;
                for (int j = 0; j < length2; j++) {
                    list.AddRange(arr2[j]);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TState, TRet>(TState state, SelectManyWhereHandler<TState, T, TRet> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                var arr2 = select(ref state, ref item);
                var length2 = arr2.Length;
                for (int j = 0; j < length2; j++) {
                    list.Add(arr2[j]);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TRet>(SelectHandlerByRef<T, TRet[]> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                var arr2 = select(ref arr[i]);
                var length2 = arr2.Length;
                for (int j = 0; j < length2; j++) {
                    list.Add(arr2[j]);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TState, TRet>(TState state, SelectHandlerByRef<TState, T, TRet[]> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                var arr2 = select(ref state, ref arr[i]);
                var length2 = arr2.Length;
                for (int j = 0; j < length2; j++) {
                    list.Add(arr2[j]);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TRet>(SelectHandlerByRef<T, IEnumerable<TRet>> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                foreach (var ret in @select(ref arr[i])) {
                    list.Add(ret);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TRet>(SelectHandlerByRef<T, StructList<TRet>> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                var t = @select(ref arr[i]);
                list.AddRange(t);
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TIn, TRet>(SelectHandlerByRef<TIn, StructList<TRet>> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                var t = @select(ref Unsafe.As<T, TIn>(ref arr[i]));
                list.AddRange(t);
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TRet[] SelectMany<TState, TRet>(TState state, SelectHandlerByRef<TState, T, IEnumerable<TRet>> select, int? knownSize = null) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TRet> list = new StructList<TRet>(knownSize ?? _count);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                foreach (var ret in @select(ref state, ref item)) {
                    list.Add(ret);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly T[] Where(PredicateHandler<T> predicate, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<T> list = new StructList<T>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref item)) {
                    list.Add(ref arr[i]);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TOut[] WhereCast<TOut>(PredicateHandler<TOut> predicate, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TOut> list = new StructList<TOut>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref Unsafe.As<T, TOut>(ref arr[i]);
                if (predicate(ref item)) {
                    list.Add(ref item);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly T[] Where<TState>(TState state, StatePredicateHandler<TState, T> predicate, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<T> list = new StructList<T>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref state, ref item)) {
                    list.Add(ref item);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly TOut[] WhereCast<TOut, TState>(TState state, StatePredicateHandler<TState, TOut> predicate, int knownSize = 16) where TOut : T {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<TOut> list = new StructList<TOut>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref state, ref Unsafe.As<T, TOut>(ref item))) {
                    list.Add(Unsafe.As<T, TOut>(ref item));
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly T[] Where<TState>(ref TState state, StatePredicateHandler<TState, T> predicate, int knownSize = 16) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            StructList<T> list = new StructList<T>(knownSize);
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                ref var item = ref arr[i];
                if (predicate(ref state, ref item)) {
                    list.Add(ref arr[i]);
                }
            }

            return list.Count == list.Capacity ? list.InternalArray : list.ToArray();
        }

        public readonly bool All(Predicate<T> predicate) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (!predicate(arr[i])) {
                    return false;
                }
            }

            return true;
        }

        public readonly bool All(PredicateByRef<T> predicate) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (!predicate(ref arr[i])) {
                    return false;
                }
            }

            return true;
        }

        public readonly bool All(PredicateHandler<T> predicate) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (!predicate(ref arr[i])) {
                    return false;
                }
            }

            return true;
        }

        public readonly bool All<TState>(TState state, StatePredicateHandler<T, TState> predicate) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (!predicate(ref arr[i], ref state)) {
                    return false;
                }
            }

            return true;
        }

        public readonly bool All<TState>(ref TState state, StatePredicateHandler<T, TState> predicate) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (!predicate(ref arr[i], ref state)) {
                    return false;
                }
            }

            return true;
        }

        public readonly bool Any() =>
            _count > 0;

        public readonly bool Any(Predicate<T> predicate) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (predicate(arr[i])) {
                    return true;
                }
            }

            return false;
        }

        public readonly bool Any(PredicateHandler<T> predicate) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (predicate(ref arr[i])) {
                    return true;
                }
            }

            return false;
        }


        public readonly bool Any<TState>(TState state, StatePredicateHandler<T, TState> predicate) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (predicate(ref arr[i], ref state)) {
                    return true;
                }
            }

            return false;
        }

        public readonly bool Any<TState>(ref TState state, StatePredicateHandler<T, TState> predicate) {
            if (_arr == null)
                throw new InvalidOperationException("StructList is uninitialized.");
            int length = _count;
            var arr = _arr;
            for (int i = 0; i < length; i++) {
                if (predicate(ref arr[i], ref state)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <returns></returns>
        /// <exception cref="ItemNotFoundException">When item of <typeparamref name="TType"/> was not found</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public readonly ref TType FindRefOfType<TType>() where TType : struct, T {
            var cnt = this._count;
            var arr = this._arr;
            if (cnt == 1) {
                if (arr[0] is TType)
                    return ref Unsafe.As<T, TType>(ref arr[0]);
                throw new ItemNotFoundException();
            }

            for (int i = 0; i < cnt; i++) {
                if (arr[i] is TType)
                    return ref Unsafe.As<T, TType>(ref arr[0]);
            }

            throw new ItemNotFoundException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public readonly TType? FindOfType<TType>() where TType : class, T {
            var cnt = this._count;
            if (cnt == 1) {
                if (_arr[0] is TType t)
                    return t;
                return default;
            }

            var arr = this._arr;
            for (int i = 0; i < cnt; i++) {
                if (arr[i] is TType t)
                    return t;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public readonly TType? FindOfType<TType>(Func<TType, bool> filter) where TType : class, T {
            var cnt = this._count;
            if (cnt == 1) {
                if (_arr[0] is TType t && filter(t))
                    return t;
                return default;
            }

            var arr = this._arr;
            for (int i = 0; i < cnt; i++) {
                if (arr[i] is TType t && filter(t))
                    return t;
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public readonly IEnumerable<TType> FindTypes<TType>() where TType : T {
            var cnt = this._count;
            var arr = this._arr;

            for (int i = 0; i < cnt; i++) {
                if (arr[i] is TType res)
                    yield return res;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public readonly IEnumerable<TType> FindTypes<TType>(Func<TType, bool> filter) where TType : T {
            var cnt = this._count;
            var arr = this._arr;

            for (int i = 0; i < cnt; i++) {
                if (arr[i] is TType res && filter(res))
                    yield return res;
            }
        }

        #region IList

        void ICollection.CopyTo(Array array, int index) {
            CopyTo((T[]) array, index);
        }


        object ICollection.SyncRoot => null;

        bool ICollection.IsSynchronized => true;

        void IList.Remove(object value) {
            Remove((T) value);
        }

        object IList.this[int index] {
            readonly get => this[index];
            set => this[index] = (T) value;
        }

        int IList.Add(object value) {
            Add((T) value);
            return 1;
        }

        readonly bool IList.Contains(object value) {
            return Contains((T) value);
        }

        readonly int IList.IndexOf(object value) {
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

        public readonly void OnAfterDeserialize() { }

        #endregion

        #region Implementation of IListView<T>

        public readonly Span<T> Span => new Span<T>(_arr, 0, _count);
        public readonly ReadOnlySpan<T> ReadOnlySpan => new ReadOnlySpan<T>(_arr, 0, _count);

        public readonly ref T GetItem(int index) {
            return ref _arr[index];
        }

        #endregion

        public void Dispose() {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                var arr = _arr;
                if (arr != null)
                    Array.Clear(arr, 0, arr.Length);
            }

            _arr = Array.Empty<T>();
            _count = 0;
        }

        public struct Enumerator : IEnumerator<T> {
            private readonly T[]? _array;
            private readonly int _start;
            private readonly int _end; // cache Offset + Count, since it's a little slow
            private int _current;

            public Enumerator(T[] array, int start, int end) {
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

            public readonly T Current {
                get {
                    #if DEBUG
                    if (_current < _start)
                        throw new InvalidOperationException("ThrowInvalidOperationException_InvalidOperation_EnumNotStarted");
                    if (_current >= _end)
                        throw new InvalidOperationException("ThrowInvalidOperationException_InvalidOperation_EnumEnded");
                    #endif
                    return _array![_current];
                }
            }

            readonly object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                _current = _start - 1;
            }

            public void Dispose() { }
        }

        /// <summary>
        ///     Explicit cast, returns an enumerator that performantly casts the value
        /// </summary>
        public readonly Enumerator<T2> Cast<T2>() {
            return new Enumerator<T2>(new CastEnumerator<T2>(_arr, 0, _count));
        }

        /// <summary>
        ///     Reinterpret cast, returns a non-copy of this list that points to given <typeparamref name="T2"/>.
        /// </summary>
        public unsafe StructList<T2> CastUnsafe<T2>() {
            return new StructList<T2>(Unsafe.As<T[], T2[]>(ref _arr), _count);
        }


        public struct Enumerator<T2> : IEnumerable<T2> {
            private CastEnumerator<T2> _enumerator;

            public Enumerator(CastEnumerator<T2> enumerator) {
                _enumerator = enumerator;
            }

            public CastEnumerator<T2> GetEnumerator() {
                return _enumerator;
            }

            IEnumerator<T2> IEnumerable<T2>.GetEnumerator() {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        public struct CastEnumerator<T2> : IEnumerator<T2> {
            private readonly T[]? _array;
            private readonly int _start;
            private readonly int _end; // cache Offset + Count, since it's a little slow
            private int _current;

            public CastEnumerator(T[] array, int start, int end) {
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

            public readonly T2 Current {
                get {
                    #if DEBUG
                    if (_current < _start)
                        throw new InvalidOperationException("ThrowInvalidOperationException_InvalidOperation_EnumNotStarted");
                    if (_current >= _end)
                        throw new InvalidOperationException("ThrowInvalidOperationException_InvalidOperation_EnumEnded");
                    #endif
                    return Unsafe.As<T, T2>(ref _array![_current]);
                }
            }

            object? IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                _current = _start - 1;
            }

            public void Dispose() { }
        }

        public readonly StructList<T> Clone() {
            return new StructList<T>((T[]) _arr.Clone(), _count);
        }

        public readonly StructList<T> CloneTrimmed() {
            if (_count == _arr.Length) {
                return new StructList<T>((T[]) _arr.Clone(), _count);
            }

            var arr = _arr;
            Array.Resize(ref arr, _count);
            return new StructList<T>(arr, _count);
        }

        public readonly T[] InternalArrayOrClone() {
            if (_count == _arr.Length) {
                return _arr;
            }

            var arr = _arr;
            Array.Resize(ref arr, _count);
            return arr;
        }

        public readonly StructList<T> Clone(int additionalCapacity) {
            #if NET6_0_OR_GREATER
            var copy = GC.AllocateUninitializedArray<T>(_arr.Length + additionalCapacity);
            #else
            var copy = new T[_arr.Length + additionalCapacity];
            #endif
            Array.Copy(_arr, 0, copy, 0, _count);
            return new StructList<T>(copy, _count);
        }


        public readonly Span<T> AsSpan() {
            return new Span<T>(_arr, 0, _count);
        }

        public readonly Span<T> AsSpan(int startIndex) {
            return new Span<T>(_arr, startIndex, _count - startIndex);
        }

        public readonly Span<T> AsSpan(int startIndex, int length) {
            return new Span<T>(_arr, startIndex, Math.Min(length, _count - startIndex));
        }

        public readonly Memory<T> AsMemory() {
            return new Memory<T>(_arr, 0, _count);
        }

        public readonly Memory<T> AsMemory(int startIndex) {
            return new Memory<T>(_arr, startIndex, _count - startIndex);
        }

        public readonly Memory<T> AsMemory(int startIndex, int length) {
            return new Memory<T>(_arr, startIndex, Math.Min(length, _count - startIndex));
        }

        public readonly ArraySegment<T> AsArraySegment() {
            return new ArraySegment<T>(_arr, 0, _count);
        }

        public readonly ArraySegment<T> AsArraySegment(int startIndex) {
            return new ArraySegment<T>(_arr, startIndex, _count - startIndex);
        }

        public readonly ArraySegment<T> AsArraySegment(int startIndex, int length) {
            return new ArraySegment<T>(_arr, startIndex, Math.Min(length, _count - startIndex));
        }
    }

    public delegate Span<TRet> SelectManyWhereHandler<T, TRet>(ref T obj);

    public delegate Span<TRet> SelectManyWhereHandler<TState, T, TRet>(ref TState state, ref T obj);

    public delegate TRet SelectHandlerByRef<T, out TRet>(ref T obj);
    public delegate ref TRet SelectHandlerByRef<TState, T, TRet>(ref TState state, ref T obj);

    public delegate TRet SelectHandler<T, out TRet>(ref T obj);
    public delegate TRet SelectHandler<TState, T, out TRet>(ref TState state, ref T obj);
    public delegate bool StatePredicateHandler<TState, T>(ref TState state, ref T item);
    public delegate bool PredicateHandler<T>(ref T item);
    public delegate bool Predicate<in T>(T item);
    public delegate bool Predicate<in TState, in T>(TState state, T item);
    public delegate bool PredicateByRef<T>(ref T item);
    public delegate bool PredicateByRef<TState, T>(ref TState state, ref T item);

    public delegate bool CompareByRef<T>(ref T lhs, ref T rhs);
}