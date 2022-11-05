using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nucs.Collections.Structs;

public struct ReusableStructListQueue<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>, IDisposable {
    #nullable disable
    private StructList<T> _array;
    private int _head;
    private int _tail;
    private int _size;

    public StructList<T> InternalArray => _array;

    #nullable enable

    public ReusableStructListQueue(StructList<T> collection, int startIndex, int count) {
        _head = startIndex;
        _array = collection;
        _size = count;
        if (count == _array.Count) {
            _tail = 0;
            return;
        }

        _tail = count;
    }

    public ReusableStructListQueue(ref StructList<T> collection, int startIndex, int count) {
        _head = startIndex;
        _array = collection;
        _size = count;
        if (count == _array.Count) {
            _tail = 0;
            return;
        }

        _tail = count;
    }

    public ReusableStructListQueue(StructList<T> collection) : this(ref collection, 0, collection.Count) { }
    public ReusableStructListQueue(ref StructList<T> collection) : this(ref collection, 0, collection.Count) { }

    public void Reuse() {
        this = new ReusableStructListQueue<T>(_array, 0, _array.Count);
    }

    public void Reuse(int startIndex, int count) {
        this = new ReusableStructListQueue<T>(_array, startIndex, count);
    }

    public void CopyTo(Array array, int index) {
        throw new NotImplementedException();
    }

    public readonly int Count => _size;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => null;

    public void Clear() {
        _size = 0;
        _head = 0;
        _tail = 0;
    }

    public Enumerator GetEnumerator() =>
        new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() =>
        new Enumerator(this);


    #nullable enable
    public T Dequeue() {
        int head = _head;
        if (_size == 0)
            throw new InvalidOperationException("SR.InvalidOperation_EmptyQueue");
        T obj = _array[head]!;
        MoveNext(ref _head);
        --_size;
        return obj!;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T result) {
        int head = _head;
        if (_size == 0) {
            result = default;
            return false;
        }

        result = _array[head];
        MoveNext(ref _head);
        --_size;
        return true;
    }

    public readonly T Peek() {
        if (_size == 0)
            throw new InvalidOperationException("SR.InvalidOperation_EmptyQueue");
        return _array[_head];
    }

    public readonly bool TryPeek([MaybeNullWhen(false)] out T result) {
        if (_size == 0) {
            result = default;
            return false;
        }

        result = _array[_head];
        return true;
    }

    #nullable disable
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveNext(ref int index) {
        int num = index + 1;
        if (num == _array.Count)
            num = 0;
        index = num;
    }


    #nullable enable
    public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator {
        #nullable disable
        private ReusableStructListQueue<T> _q;
        private int _index;
        private T _currentElement;

        internal Enumerator(ReusableStructListQueue<T> q) {
            _q = q;
            _index = -1;
            _currentElement = default;
        }

        public void Dispose() {
            _index = -2;
            _currentElement = default;
        }

        public bool MoveNext() {
            if (_index == -2)
                return false;
            ++_index;
            if (_index == _q._size) {
                _index = -2;
                _currentElement = default;
                return false;
            }

            ref var array = ref _q._array;
            int length = array.Count;
            int index = _q._head + _index;
            if (index >= length)
                index -= length;
            _currentElement = array[index];
            return true;
        }


        #nullable enable
        public T Current {
            get {
                if (_index < 0)
                    ThrowEnumerationNotStartedOrEnded();
                return _currentElement;
            }
        }

        private void ThrowEnumerationNotStartedOrEnded() =>
            throw new InvalidOperationException(_index == -1 ? "SR.InvalidOperation_EnumNotStarted" : "SR.InvalidOperation_EnumEnded");

        object? IEnumerator.Current => Current;

        void IEnumerator.Reset() {
            _index = -1;
            _currentElement = default;
        }
    }

    internal static T[] ToArray<T>(IEnumerable<T> source, out int length) {
        if (source is ICollection<T> objs) {
            int count = objs.Count;
            if (count != 0) {
                T[] array = new T[count];
                objs.CopyTo(array, 0);
                length = count;
                return array;
            }
        } else {
            using (IEnumerator<T> enumerator = source.GetEnumerator()) {
                if (enumerator.MoveNext()) {
                    T[] array = new T[4] {
                        enumerator.Current,
                        default,
                        default,
                        default
                    };
                    int num = 1;
                    while (enumerator.MoveNext()) {
                        if (num == array.Length) {
                            int newSize = num << 1;
                            if ((uint) newSize > 2146435071U)
                                newSize = 2146435071 <= num ? num + 1 : 2146435071;
                            Array.Resize<T>(ref array, newSize);
                        }

                        array[num++] = enumerator.Current;
                    }

                    length = num;
                    return array;
                }
            }
        }

        length = 0;
        return Array.Empty<T>();
    }

    #region IDisposable

    public void Dispose() {
        _head = _size = _tail = 0;
    }

    #endregion
}