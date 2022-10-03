#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nucs.Collections {
    // Decompiled with JetBrains decompiler
    // Type: System.Collections.Generic.ExposedQueue`1
    // Assembly: System.Collections, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
    // MVID: 9A40C45A-BB02-45B9-9BD3-DCD356A94F97
    // Assembly location: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\5.0.7\System.Collections.dll


    internal sealed class ExposedQueueDebugView<T> {
        private readonly ExposedQueue<T> _queue;

        public ExposedQueueDebugView(ExposedQueue<T> queue) =>
            this._queue = queue != null ? queue : throw new ArgumentNullException(nameof(queue));

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => this._queue.ToArray();
    }

    [DebuggerTypeProxy(typeof(ExposedQueueDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    [Serializable]
    public class ExposedQueue<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T> {
        #nullable disable
        private T[] _array;
        private int _head;
        private int _tail;
        private int _size;
        private int _version;

        public ExposedQueue() =>
            this._array = Array.Empty<T>();

        public ExposedQueue(int capacity) =>
            this._array = capacity >= 0 ? new T[capacity] : throw new ArgumentOutOfRangeException(nameof(capacity), (object) capacity, "SR.ArgumentOutOfRange_NeedNonNegNum");


        #nullable enable
        public ExposedQueue(IEnumerable<T> collection) {
            this._array = collection != null ? ToArray<T>(collection, out this._size) : throw new ArgumentNullException(nameof(collection));
            if (this._size == this._array.Length)
                return;
            this._tail = this._size;
        }

        public int Count => this._size;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => (object) this;

        public void Clear() {
            if (this._size != 0) {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                    if (this._head < this._tail) {
                        Array.Clear((Array) this._array, this._head, this._size);
                    } else {
                        Array.Clear((Array) this._array, this._head, this._array.Length - this._head);
                        Array.Clear((Array) this._array, 0, this._tail);
                    }
                }

                this._size = 0;
            }

            this._head = 0;
            this._tail = 0;
            ++this._version;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), (object) arrayIndex, "SR.ArgumentOutOfRange_Index");
            if (array.Length - arrayIndex < this._size)
                throw new ArgumentException("SR.Argument_InvalidOffLen");
            int size = this._size;
            if (size == 0)
                return;
            int length1 = Math.Min(this._array.Length - this._head, size);
            Array.Copy((Array) this._array, this._head, (Array) array, arrayIndex, length1);
            int length2 = size - length1;
            if (length2 <= 0)
                return;
            Array.Copy((Array) this._array, 0, (Array) array, arrayIndex + this._array.Length - this._head, length2);
        }


        #nullable disable
        void ICollection.CopyTo(Array array, int index) {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1)
                throw new ArgumentException("SR.Arg_RankMultiDimNotSupported", nameof(array));
            int num = array.GetLowerBound(0) == 0 ? array.Length : throw new ArgumentException("SR.Arg_NonZeroLowerBound", nameof(array));
            if (index < 0 || index > num)
                throw new ArgumentOutOfRangeException(nameof(index), (object) index, "SR.ArgumentOutOfRange_Index");
            if (num - index < this._size)
                throw new ArgumentException("SR.Argument_InvalidOffLen");
            int size = this._size;
            if (size == 0)
                return;
            try {
                int length1 = this._array.Length - this._head < size ? this._array.Length - this._head : size;
                Array.Copy((Array) this._array, this._head, array, index, length1);
                int length2 = size - length1;
                if (length2 <= 0)
                    return;
                Array.Copy((Array) this._array, 0, array, index + this._array.Length - this._head, length2);
            } catch (ArrayTypeMismatchException ex) {
                throw new ArgumentException("SR.Argument_InvalidArrayType", nameof(array));
            }
        }


        #nullable enable
        public void Enqueue(T item) {
            if (this._size == this._array.Length) {
                int capacity = (int) ((long) this._array.Length * 200L / 100L);
                if (capacity < this._array.Length + 4)
                    capacity = this._array.Length + 4;
                this.SetCapacity(capacity);
            }

            this._array[this._tail] = item;
            this.MoveNext(ref this._tail);
            ++this._size;
            ++this._version;
        }

        public ExposedQueue<
            #nullable disable
            T>.Enumerator GetEnumerator() =>
            new ExposedQueue<T>.Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
            (IEnumerator<T>) new ExposedQueue<T>.Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() =>
            (IEnumerator) new ExposedQueue<T>.Enumerator(this);


        #nullable enable
        public T Dequeue() {
            int head = this._head;
            T[] array = this._array;
            if (this._size == 0)
                this.ThrowForEmptyQueue();
            T obj = array[head];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                array[head] = default(T);
            this.MoveNext(ref this._head);
            --this._size;
            ++this._version;
            return obj;
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T result) {
            int head = this._head;
            T[] array = this._array;
            if (this._size == 0) {
                result = default(T);
                return false;
            }

            result = array[head];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                array[head] = default(T);
            this.MoveNext(ref this._head);
            --this._size;
            ++this._version;
            return true;
        }

        public T Peek() {
            if (this._size == 0)
                this.ThrowForEmptyQueue();
            return this._array[this._head];
        }

        public bool TryPeek([MaybeNullWhen(false)] out T result) {
            if (this._size == 0) {
                result = default(T);
                return false;
            }

            result = this._array[this._head];
            return true;
        }

        public bool Contains(T item) {
            if (this._size == 0)
                return false;
            if (this._head < this._tail)
                return Array.IndexOf<T>(this._array, item, this._head, this._size) >= 0;
            return Array.IndexOf<T>(this._array, item, this._head, this._array.Length - this._head) >= 0 || Array.IndexOf<T>(this._array, item, 0, this._tail) >= 0;
        }

        public T[] ToArray() {
            if (this._size == 0)
                return Array.Empty<T>();
            T[] objArray = new T[this._size];
            if (this._head < this._tail) {
                Array.Copy((Array) this._array, this._head, (Array) objArray, 0, this._size);
            } else {
                Array.Copy((Array) this._array, this._head, (Array) objArray, 0, this._array.Length - this._head);
                Array.Copy((Array) this._array, 0, (Array) objArray, this._array.Length - this._head, this._tail);
            }

            return objArray;
        }

        private void SetCapacity(int capacity) {
            T[] objArray = new T[capacity];
            if (this._size > 0) {
                if (this._head < this._tail) {
                    Array.Copy((Array) this._array, this._head, (Array) objArray, 0, this._size);
                } else {
                    Array.Copy((Array) this._array, this._head, (Array) objArray, 0, this._array.Length - this._head);
                    Array.Copy((Array) this._array, 0, (Array) objArray, this._array.Length - this._head, this._tail);
                }
            }

            this._array = objArray;
            this._head = 0;
            this._tail = this._size == capacity ? 0 : this._size;
            ++this._version;
        }


        #nullable disable
        private void MoveNext(ref int index) {
            int num = index + 1;
            if (num == this._array.Length)
                num = 0;
            index = num;
        }

        private void ThrowForEmptyQueue() =>
            throw new InvalidOperationException("SR.InvalidOperation_EmptyQueue");

        public void TrimExcess() {
            if (this._size >= (int) ((double) this._array.Length * 0.9))
                return;
            this.SetCapacity(this._size);
        }


        #nullable enable
        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator {
            #nullable disable
            private readonly ExposedQueue<T> _q;
            private readonly int _version;
            private int _index;
            private T _currentElement;

            internal Enumerator(ExposedQueue<T> q) {
                this._q = q;
                this._version = q._version;
                this._index = -1;
                this._currentElement = default(T);
            }

            public void Dispose() {
                this._index = -2;
                this._currentElement = default(T);
            }

            public bool MoveNext() {
                if (this._version != this._q._version)
                    throw new InvalidOperationException("SR.InvalidOperation_EnumFailedVersion");
                if (this._index == -2)
                    return false;
                ++this._index;
                if (this._index == this._q._size) {
                    this._index = -2;
                    this._currentElement = default(T);
                    return false;
                }

                T[] array = this._q._array;
                int length = array.Length;
                int index = this._q._head + this._index;
                if (index >= length)
                    index -= length;
                this._currentElement = array[index];
                return true;
            }


            #nullable enable
            public T Current {
                get {
                    if (this._index < 0)
                        this.ThrowEnumerationNotStartedOrEnded();
                    return this._currentElement;
                }
            }

            private void ThrowEnumerationNotStartedOrEnded() =>
                throw new InvalidOperationException(this._index == -1 ? "SR.InvalidOperation_EnumNotStarted" : "SR.InvalidOperation_EnumEnded");

            object? IEnumerator.Current => (object) this.Current;

            void IEnumerator.Reset() {
                if (this._version != this._q._version)
                    throw new InvalidOperationException("SR.InvalidOperation_EnumFailedVersion");
                this._index = -1;
                this._currentElement = default(T);
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
    }
}