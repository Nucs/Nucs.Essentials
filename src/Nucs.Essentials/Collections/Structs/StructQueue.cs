using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Nucs.Extensions;

namespace Nucs.Collections.Structs {
    // Decompiled with JetBrains decompiler
    // Type: System.Collections.Generic.StructQueue`1
    // Assembly: System.Collections, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
    // MVID: 9A40C45A-BB02-45B9-9BD3-DCD356A94F97
    // Assembly location: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\5.0.7\System.Collections.dll

    internal sealed class QueueDebugView<T> {
        private readonly StructQueue<T> _queue;

        public QueueDebugView(StructQueue<T> queue) =>
            _queue = queue;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _queue.ToArray();
    }

    #nullable enable
    [DebuggerTypeProxy(typeof(QueueDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public struct StructQueue<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>, IDisposable {
        #nullable disable
        private T[] _array;
        private int _head;
        private int _tail;
        private int _size;

        public T[] InternalArray => _array;

        public StructQueue(int capacity) {
            _array = capacity >= 0 ? new T[capacity] : capacity == 0 ? Array.Empty<T>() : throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "SR.ArgumentOutOfRange_NeedNonNegNum");
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        #nullable enable
        public StructQueue(IEnumerable<T> collection) {
            _head = 0;
            _array = collection != null ? ToArray<T>(collection, out _size) : throw new ArgumentNullException(nameof(collection));
            if (_size == _array.Length) {
                _tail = 0;
                return;
            }

            _tail = _size;
        }

        public StructQueue(IList<T> collection) {
            _head = 0;
            _array = collection.ToArrayFast(collection.Count);
            _size = collection.Count;
            if (_size == _array.Length) {
                _tail = 0;
                return;
            }

            _tail = _size;
        }

        public StructQueue(List<T> collection) {
            _head = 0;
            _array = collection.ToArrayFast(collection.Count);
            _size = collection.Count;
            if (_size == _array.Length) {
                _tail = 0;
                return;
            }

            _tail = _size;
        }

        public StructQueue(T[] collection, int startIndex, int count) {
            _head = startIndex;
            _array = collection;
            _size = count;
            if (count == _array.Length) {
                _tail = 0;
                return;
            }

            _tail = count;
        }

        public StructQueue(T[] collection) : this(collection, 0, collection.Length) { }

        public StructQueue(StructList<T> collection) : this((T[]) collection.InternalArray.Clone(), 0, collection.Length) { }

        public readonly int Count => _size;
        public readonly int Capacity => _array.Length;
        public readonly bool IsEmpty => _size == 0;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => null;

        public ref T this[int index] {
            get {
                #if DEBUG
                if (index >= _size)
                    throw new ArgumentOutOfRangeException(nameof(index));
                #endif
                var target = _head + index;
                if (target < _tail) {
                    return ref _array[target];
                } else {
                    return ref _array[target % _size]; //0-5, _head:3, index:3 -> 6 % 5 -> [1]
                }
            }
        }
        
        public void Clear() {
            if (_size != 0) {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                    if (_head < _tail) {
                        Array.Clear(_array, _head, _size);
                    } else {
                        Array.Clear(_array, _head, _array.Length - _head);
                        Array.Clear(_array, 0, _tail);
                    }
                }

                _size = 0;
            }

            _head = 0;
            _tail = 0;
        }

        public readonly void CopyTo(T[] array, int arrayIndex) {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "SR.ArgumentOutOfRange_Index");
            if (array.Length - arrayIndex < _size)
                throw new ArgumentException("SR.Argument_InvalidOffLen");
            int size = _size;
            if (size == 0)
                return;
            int length1 = Math.Min(_array.Length - _head, size);
            Array.Copy(_array, _head, array, arrayIndex, length1);
            int length2 = size - length1;
            if (length2 <= 0)
                return;
            Array.Copy(_array, 0, array, arrayIndex + _array.Length - _head, length2);
        }


        #nullable disable
        void ICollection.CopyTo(Array array, int index) {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException("SR.Arg_RankMultiDimNotSupported", nameof(array));
            int num = array.GetLowerBound(0) == 0 ? array.Length : throw new ArgumentException("SR.Arg_NonZeroLowerBound", nameof(array));
            if (index < 0 || index > num)
                throw new ArgumentOutOfRangeException(nameof(index), index, "SR.ArgumentOutOfRange_Index");
            if (num - index < _size)
                throw new ArgumentException("SR.Argument_InvalidOffLen");
            int size = _size;
            if (size == 0)
                return;
            try {
                int length1 = _array.Length - _head < size ? _array.Length - _head : size;
                Array.Copy(_array, _head, array, index, length1);
                int length2 = size - length1;
                if (length2 <= 0)
                    return;
                Array.Copy(_array, 0, array, index + _array.Length - _head, length2);
            } catch (ArrayTypeMismatchException ex) {
                throw new ArgumentException("SR.Argument_InvalidArrayType", nameof(array));
            }
        }

        public void Enqueue(T item) {
            if (_size == _array.Length) {
                int capacity = (int) (_array.Length * 200L / 100L);
                if (capacity < _array.Length + 4)
                    capacity = _array.Length + 4;
                SetCapacity(capacity);
            }

            _array[_tail] = item;
            MoveNext(ref _tail);
            ++_size;
        }

        public void EnqueueFirstOut(T item) {
            if (_size == _array.Length) {
                int capacity = (int) (_array.Length * 200L / 100L);
                if (capacity < _array.Length + 4)
                    capacity = _array.Length + 4;
                SetCapacity(capacity);
            }

            //if head is 0 then we got to try to go to end or enlarge
            MoveBack(ref _head);
            _array[_head] = item;
            ++_size;
        }

        public void Enqueue(ref T item) {
            if (_size == _array.Length) {
                int capacity = (int) (_array.Length * 200L / 100L);
                if (capacity < _array.Length + 4)
                    capacity = _array.Length + 4;
                SetCapacity(capacity);
            }

            _array[_tail] = item;
            MoveNext(ref _tail);
            ++_size;
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
            T[] array = _array;
            if (_size == 0)
                throw new InvalidOperationException("SR.InvalidOperation_EmptyQueue");
            T obj = array[head]!;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                array[head] = default!;
            MoveNext(ref _head);
            --_size;
            return obj!;
        }

        public ref T PeakRef() {
            if (_size == 0)
                throw new InvalidOperationException("SR.InvalidOperation_EmptyQueue");

            return ref _array[_head]!;
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T result) {
            int head = _head;
            T[] array = _array;
            if (_size == 0) {
                result = default;
                return false;
            }

            result = array[head];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                array[head] = default!;
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

        public readonly bool Contains(T item) {
            if (_size == 0)
                return false;
            if (_head < _tail)
                return Array.IndexOf<T>(_array, item, _head, _size) >= 0;
            return Array.IndexOf<T>(_array, item, _head, _array.Length - _head) >= 0 || Array.IndexOf<T>(_array, item, 0, _tail) >= 0;
        }

        public readonly T[] ToArray() {
            if (_size == 0)
                return Array.Empty<T>();
            T[] objArray = new T[_size];
            if (_head < _tail) {
                Array.Copy(_array, _head, objArray, 0, _size);
            } else {
                Array.Copy(_array, _head, objArray, 0, _array.Length - _head);
                Array.Copy(_array, 0, objArray, _array.Length - _head, _tail);
            }

            return objArray;
        }

        public readonly void CopyTo(T[] objArray) {
            if (_size == 0)
                return;
            if (_head < _tail) {
                Array.Copy(_array, _head, objArray, 0, _size);
            } else {
                Array.Copy(_array, _head, objArray, 0, _array.Length - _head);
                Array.Copy(_array, 0, objArray, _array.Length - _head, _tail);
            }
        }

        public readonly void CopyTo(Span<T> objArray) {
            if (_size == 0)
                return;
            if (_head < _tail) {
                _array.AsSpan(_head).CopyTo(objArray.Slice(0, _size));
            } else {
                _array.AsSpan(_head).CopyTo(objArray.Slice(0, _array.Length - _head));
                _array.AsSpan().CopyTo(objArray.Slice(_array.Length - _head, _tail));
            }
        }

        public void EnsureCapacity(int capacity) {
            if (capacity <= Capacity)
                return;
            SetCapacity(capacity);
        }

        public void SetCapacity(int capacity) {
            T[] objArray = new T[capacity];
            if (_size > 0) {
                if (_head < _tail) {
                    Array.Copy(_array, _head, objArray, 0, _size);
                } else {
                    Array.Copy(_array, _head, objArray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, objArray, _array.Length - _head, _tail);
                }
            }

            _array = objArray;
            _head = 0;
            _tail = _size == capacity ? 0 : _size;
        }


        #nullable disable
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveNext(ref int index) {
            int num = index + 1;
            if (num == _array.Length)
                num = 0;
            index = num;
        }

        #nullable disable
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveBack(ref int index) {
            int num = index - 1;
            if (num == -1)
                num = _array.Length - 1;
            index = num;
        }

        public void TrimExcess() {
            if (_size >= (int) (_array.Length * 0.9))
                return;
            SetCapacity(_size);
        }


        #nullable enable
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator {
            #nullable disable
            private readonly StructQueue<T> _q;
            private int _index;
            private T _currentElement;

            internal Enumerator(StructQueue<T> q) {
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

                T[] array = _q._array;
                int length = array.Length;
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
            Array.Clear(_array, 0, _array.Length);
            _array = Array.Empty<T>();
            _head = _size = _tail = 0;
        }

        #endregion
    }
}