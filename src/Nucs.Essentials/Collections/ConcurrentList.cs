using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Nucs.Exceptions;

namespace Nucs.Collections {

    public class ConcurrentList<T> : IList<T>, IReadOnlyList<T>, IList, IDisposable {
        private readonly ReaderWriterLockSlim _lock;
        
        public ref T?[] InternalArray => ref _arr!;

        internal int _count = 0;

        internal T[] _arr;
        
        internal Span<T> AsUnlockedSpan => new Span<T>(_arr, 0, _count);
        
        public ReaderWriterLockSlim Lock => _lock;

        public int Count {
            get {
                _lock.EnterReadLock();
                try {
                    return _count;
                } finally {
                    _lock.ExitReadLock();
                }
            }
        }
        
        public int Capacity {
            get {
                _lock.EnterReadLock();
                try {
                    return _arr.Length;
                } finally {
                    _lock.ExitReadLock();
                }
            }
        }

        public ConcurrentList(T[] array, int count, ReaderWriterLockSlim @lock) {
            _arr = array;
            _count = count;
            _lock = @lock;
        }

        
        public ConcurrentList(T[] array, int count) : this(array, count, new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion)) { }

        public ConcurrentList(int initialCapacity) : this(initialCapacity, new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion)) { }

        public ConcurrentList() : this(4) { }

        public ConcurrentList(int initialCapacity, ReaderWriterLockSlim @lock) : this(new T[initialCapacity], 0, @lock) { }

        public ConcurrentList(ReaderWriterLockSlim @lock) : this(4, @lock) { }

        public ConcurrentList(IEnumerable<T> items) {
            _arr = items.ToArray();
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            _count = _arr.Length;
        }

        public void Add(T item) {
            try {
                _lock.EnterWriteLock();
            } catch (ObjectDisposedException) {
                //swallow
            }

            try {
                var newCount = _count + 1;
                EnsureCapacity(newCount);
                _arr[_count] = item;
                _count = newCount;
            } finally {
                try {
                    _lock.ExitWriteLock();
                } catch (ObjectDisposedException) {
                    //swallow
                }
            }
        }
        
        public int AddToEnd(T item) {
            try {
                _lock.EnterWriteLock();
            } catch (ObjectDisposedException) {
                //swallow
            }

            var addingTo = _count;
            try {
                var newCount = addingTo + 1;
                EnsureCapacity(newCount);
                _arr[addingTo] = item;
                _count = newCount;
            } finally {
                try {
                    _lock.ExitWriteLock();
                } catch (ObjectDisposedException) {
                    //swallow
                }
            }

            return addingTo;
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

            _lock.EnterWriteLock();

            try {
                var arr = items as T[] ?? items.ToArray();
                var newCount = _count + arr.Length;
                EnsureCapacity(newCount);
                Array.Copy(arr, 0, _arr, _count, arr.Length);
                _count = newCount;
            } finally {
                _lock.ExitWriteLock();
            }
        }

        public void AddRange(T[] items) {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            _lock.EnterWriteLock();

            try {
                var newCount = _count + items.Length;
                EnsureCapacity(newCount);
                Array.Copy(items, 0, _arr, _count, items.Length);
                _count = newCount;
            } finally {
                _lock.ExitWriteLock();
            }
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
            _lock.EnterUpgradeableReadLock();

            try {
                var i = IndexOfInternal(item);

                if (i == -1)
                    return false;

                _lock.EnterWriteLock();
                try {
                    RemoveAtInternal(i);
                    return true;
                } finally {
                    _lock.ExitWriteLock();
                }
            } finally {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public IEnumerator<T> GetEnumerator() {
            _lock.EnterUpgradeableReadLock();
            try {
                var cnt = Count;
                for (int i = 0; i < Math.Min(cnt, Count); i++) {
                    yield return _arr[i];
                }
            } finally {
                _lock.ExitUpgradeableReadLock();
            }
        }

        internal IEnumerable<T> GetUnlockedEnumerator() {
            for (int i = 0; i < _count; i++)
                yield return _arr[i];
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public int IndexOf(T item) {
            _lock.EnterReadLock();
            try {
                return IndexOfInternal(item);
            } finally {
                _lock.ExitReadLock();
            }
        }

        private int IndexOfInternal(T item) {
            for (int i = 0; i < _count; i++) {
                if (_arr[i].Equals(item))
                    return i;
            }

            return -1;
        }

        public void Insert(int index, T item) {
            _lock.EnterUpgradeableReadLock();

            try {
                if (index > _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                _lock.EnterWriteLock();
                try {
                    var newCount = _count + 1;
                    EnsureCapacity(newCount);

                    // shift everything right by one, starting at index
                    Array.Copy(_arr, index, _arr, index + 1, _count - index);

                    // insert
                    _arr[index] = item;
                    _count = newCount;
                } finally {
                    _lock.ExitWriteLock();
                }
            } finally {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public void RemoveAt(int index) {
            _lock.EnterUpgradeableReadLock();
            try {
                if (index >= _count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                _lock.EnterWriteLock();
                try {
                    RemoveAtInternal(index);
                } finally {
                    _lock.ExitWriteLock();
                }
            } finally {
                _lock.ExitUpgradeableReadLock();
            }
        }

        private void RemoveAtInternal(int index) {
            Array.Copy(_arr, index + 1, _arr, index, _count - index - 1);
            _count--;

            // release last element
            Array.Clear(_arr, _count, 1);
        }


        public void Clear() {
            _lock.EnterWriteLock();
            try {
                Array.Clear(_arr, 0, _count);
                _count = 0;
            } finally {
                _lock.ExitWriteLock();
            }
        }

        public T[] ToArray() {
            _lock.EnterReadLock();
            try {
                if (_count == 0)
                    return Array.Empty<T>();

                var ret = new T[_count];
                Array.Copy(_arr, ret, _count);
                return ret;
            } finally {
                _lock.ExitReadLock();
            }
        }

        public bool Contains(T item) {
            _lock.EnterReadLock();
            try {
                return IndexOfInternal(item) != -1;
            } finally {
                _lock.ExitReadLock();
            }
        }

        public bool Contains(T item, Func<T, T, bool> comparer) {
            _lock.EnterReadLock();
            try {
                for (long i = 0; i < _count; i++) {
                    ref var ptr = ref _arr[i];
                    if (comparer(item, ptr)) {
                        return true;
                    }
                }

                return false;
            } finally {
                _lock.ExitReadLock();
            }
        }

        public bool Contains(Func<T, bool> comparer) {
            _lock.EnterReadLock();
            try {
                for (long i = 0; i < _count; i++) {
                    ref var ptr = ref _arr[i];
                    if (comparer(ptr)) {
                        return true;
                    }
                }

                return false;
            } finally {
                _lock.ExitReadLock();
            }
        }

        public bool TryGetValue(out T item, Func<T, bool> selector) {
            _lock.EnterReadLock();
            try {
                for (long i = 0; i < _count; i++) {
                    ref var ptr = ref _arr[i];
                    if (selector(ptr)) {
                        item = ptr;
                        return true;
                    }
                }

                item = default;
                return false;
            } finally {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        ///     Tries to add the object.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="comparer">Comparer to identify if this item already exists.</param>
        /// <returns>True if object added</returns>
        public bool TryAdd(T item, Func<T, T, bool> comparer) {
            _lock.EnterUpgradeableReadLock();
            try {
                for (long i = 0; i < _count; i++) {
                    ref var ptr = ref _arr[i];
                    if (comparer(ptr, item)) {
                        return false;
                    }
                }

                _lock.EnterWriteLock();
                try {
                    var newCount = _count + 1;
                    EnsureCapacity(newCount);
                    _arr[_count] = item;
                    _count = newCount;
                } finally {
                    _lock.ExitWriteLock();
                }

                return true;
            } finally {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        ///     Tries to add the object, if object is found then it will be replaced (first occurance).
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="comparer">Comparer to identify if this item already exists.</param>
        /// <returns>True if object added</returns>
        public void TryAddOrReplace(T item, Func<T, T, bool> comparer) {
            _lock.EnterUpgradeableReadLock();
            try {
                for (long i = 0; i < _count; i++) {
                    ref var ptr = ref _arr[i];
                    if (comparer(ptr, item)) {
                        ptr = item;
                        return;
                    }
                }

                _lock.EnterWriteLock();
                try {
                    var newCount = _count + 1;
                    EnsureCapacity(newCount);
                    _arr[_count] = item;
                    _count = newCount;
                } finally {
                    _lock.ExitWriteLock();
                }
            } finally {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        ///     Tries to add the object.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="comparer">Comparer to identify if this item already exists.</param>
        /// <returns>True if object added</returns>
        public bool TryAdd(T item, Func<T, bool> comparer) {
            _lock.EnterUpgradeableReadLock();
            try {
                for (long i = 0; i < _count; i++) {
                    ref var ptr = ref _arr[i];
                    if (comparer(ptr)) {
                        return false;
                    }
                }

                Add(item);
                return true;
            } finally {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public bool TryAdd(Func<T> factory, Func<T, bool> comparer) {
            _lock.EnterUpgradeableReadLock();
            try {
                for (long i = 0; i < _count; i++) {
                    ref var ptr = ref _arr[i];
                    if (comparer(ptr)) {
                        return false;
                    }
                }

                Add(factory());
                return true;
            } finally {
                _lock.ExitUpgradeableReadLock();
            }
        }


        public T Find(Func<T, bool> selector) {
            _lock.EnterReadLock();
            try {
                for (long i = 0; i < _count; i++) {
                    ref var ptr = ref _arr[i];
                    if (selector(ptr)) {
                        return ptr;
                    }
                }

                throw new ItemNotFoundException("Unable to find item using selector: " + selector);
            } finally {
                _lock.ExitReadLock();
            }
        }

        public T FindOrDefault(Func<T, bool> selector, T @default = default) {
            _lock.EnterReadLock();
            try {
                for (long i = 0; i < _count; i++) {
                    ref var ptr = ref _arr[i];
                    if (selector(ptr)) {
                        return ptr;
                    }
                }

                return @default;
            } finally {
                _lock.ExitReadLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex) {
            _lock.EnterReadLock();
            try {
                if (_count > array.Length - arrayIndex)
                    throw new ArgumentException("Destination array was not long enough.");

                Array.Copy(_arr, 0, array, arrayIndex, _count);
            } finally {
                _lock.ExitReadLock();
            }
        }

        
        public bool IsReadOnly {
            get { return false; }
        }

        
        bool IList.IsFixedSize { get; }

        
        public T this[int index] {
            get {
                _lock.EnterReadLock();
                try {
                    if (index >= _count)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    return _arr[index];
                } finally {
                    _lock.ExitReadLock();
                }
            }
            set {
                _lock.EnterUpgradeableReadLock();
                try {
                    if (index >= _count)
                        throw new ArgumentOutOfRangeException(nameof(index));

                    _lock.EnterWriteLock();
                    try {
                        _arr[index] = value;
                    } finally {
                        _lock.ExitWriteLock();
                    }
                } finally {
                    _lock.ExitUpgradeableReadLock();
                }
            }
        }

        public void Trim() {
            _lock.EnterUpgradeableReadLock();
            try {
                if (_count == _arr.Length)
                    return;

                _lock.EnterWriteLock();
                try {
                    Array.Resize(ref _arr, _count);
                } finally {
                    _lock.ExitWriteLock();
                }
            } finally {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public void DoSync(Action<ConcurrentList<T>> action) {
            GetSync(l => {
                action(l);
                return 0;
            });
        }

        public TResult GetSync<TResult>(Func<ConcurrentList<T>, TResult> func) {
            _lock.EnterWriteLock();
            try {
                return func(this);
            } finally {
                _lock.ExitWriteLock();
            }
        }

        public T Last() {
            _lock.EnterReadLock();
            if (_count == 0)
                throw new ItemNotFoundException();
            try {
                return _arr[_count - 1];
            } finally {
                _lock.ExitReadLock();
            }
        }

        public T First() {
            _lock.EnterReadLock();
            if (_count == 0)
                throw new ItemNotFoundException();
            try {
                return _arr[0];
            } finally {
                _lock.ExitReadLock();
            }
        }

        public void Dispose() {
            _lock.Dispose();
        }

        #region IList

        void ICollection.CopyTo(Array array, int index) {
            CopyTo((T[]) array, index);
        }


        object ICollection.SyncRoot {
            get { return _lock; }
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
    }
}