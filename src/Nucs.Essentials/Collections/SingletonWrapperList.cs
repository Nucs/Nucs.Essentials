using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Nucs.Collections {
    public struct SingletonWrapperList<T> : IList<T> {
        private bool _empty;

        private T _item;

        public SingletonWrapperList(T item) {
            _empty = false;
            _item = item;
        }

        #region Implementation of IEnumerable

        public IEnumerator<T> GetEnumerator() {
            return new Enumerator(_item);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        #region Implementation of ICollection<T>

        public void Add(T item) {
            if (!_empty) throw new NotSupportedException();
            _empty = false;
            _item = item;
        }

        public void Clear() {
            _item = default;
            _empty = true;
        }

        public bool Contains(T item) {
            return Equals(_item, item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            array[arrayIndex] = _item;
        }

        public bool Remove(T item) {
            if (Contains(item)) {
                _empty = true;
                return true;
            }

            return false;
        }

        public int Count => _empty ? 0 : 1;
        public bool IsReadOnly => false;

        #endregion

        #region Implementation of IList<T>

        public int IndexOf(T item) {
            return Contains(item) ? 0 : -1;
        }

        public void Insert(int index, T item) {
            if (!_empty || index != 0) throw new NotSupportedException();

            Add(item);
        }

        public void RemoveAt(int index) {
            Debug.Assert(index == 0);
            Clear();
        }

        public T this[int index] {
            get {
                Debug.Assert(index == 0);
                return _item;
            }
            set {
                Debug.Assert(index == 0);
                if (_empty)
                    _empty = false;
                _item = value;
            }
        }

        #endregion

        private struct Enumerator : IEnumerator<T> {
            #region Implementation of IDisposable

            private bool _done;

            public Enumerator(T current) {
                Current = current;
                _done = false;
            }

            public void Dispose() { }

            #endregion

            #region Implementation of IEnumerator

            public bool MoveNext() {
                if (_done)
                    return false;
                _done = true;
                return true;
            }

            public void Reset() {
                _done = false;
            }

            public T Current { get; }

            object IEnumerator.Current => Current;

            #endregion
        }
    }
}