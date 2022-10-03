// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Nucs.Collections.Structs;

//WHAT IF WE CAST TO A LAYOUT that is T : struct and have SUPER OPTIMIZED METHODS
namespace Nucs.Collections.Layouts {
    //WITH BCL WE CAN CREATE A SET OF METHODS THAT"LL ALLOW TO REINTERPRET A LIST!
    //CAN I JUST REINTERPRET THE T? 
    public class Relist<T, TFrom> {
        public T[] Items;
        public int Size;
        public int _version;

        #if !(NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0)
        #pragma warning disable 649
        public Object SyncRoot;
        #pragma warning restore 649
        #endif


        public int Length => Unsafe.SizeOf<TFrom>() / Unsafe.SizeOf<T>() * Size;
    }

    public class ListLayoutStruct<T> where T : struct, IComparable {
        public T[] Items;
        public int Size;
        public int _version;

        #if !(NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0)
        #pragma warning disable 649
        public Object SyncRoot;
        #pragma warning restore 649
        #endif

        public void Insert(int index, T item) {
            if ((uint) index > (uint) this.Size)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (this.Size == this.Items.Length)
                this.EnsureCapacity(this.Size + 1);
            /*fixed (T* arr = Items)*/
            if (index < this.Size)
                Array.Copy((Array) this.Items, index, (Array) this.Items, index + 1, this.Size - index);
            this.Items[index] = item;
            ++this.Size;
            ++this._version;
        }

        public void EnsureCapacity(int min) {
            if (Items.Length >= min)
                return;
            int num = Items.Length == 0 ? 4 : Items.Length * 2;
            if ((uint) num > 2146435071U)
                num = 2146435071;
            if (num < min)
                num = min;
            Size = num;
        }
    }

    public class ListLayout<T> {
        public T[] _items;
        public int Size;
        public int _version;

        #if !(NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0)
        #pragma warning disable 649
        public Object SyncRoot;
        #pragma warning restore 649
        #endif

        public ref T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref _items[index];
        }

        public ref T this[uint index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref _items[index];
        }

        public ref T this[long index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref _items[index];
        }

        public void Deconstruct(out T[] arr, out int count) {
            arr = _items;
            count = Size;
        }

        /// <summary>
        ///     Extracts internal array as a struct list.
        /// </summary>
        /// <returns></returns>
        public StructList<T> AsStructList() =>
            new StructList<T>(_items, Size);

        public ref T GetPinnableReference() {
            return ref _items[0];
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
            if ((uint) index > (uint) this.Size) throw new ArgumentOutOfRangeException(nameof(index));
            if (collection is IList<T> objs) {
                int count = objs.Count;
                if (count > 0) {
                    this.EnsureCapacity(_items.Length + count);
                    if (index < this.Size)
                        Array.Copy(this._items, index, this._items, index + count, this.Size - index);

                    if (this == objs) {
                        // Copy first part of _items to insert location
                        Array.Copy(_items, 0, _items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(_items, index + count, _items, index * 2, this.Size - index);
                    } else {
                        objs.CopyTo(_items, index);
                    }

                    this.Size += count;
                }
            } else {
                foreach (T obj in collection)
                    this.Insert(index++, obj);
            }

            ++this._version;
        }

        public static void InsertRange(ref T[] _items, ref int Size, int index, IList<T> objs) {
            if (objs == null) throw new ArgumentNullException(nameof(objs));
            if ((uint) index > (uint) Size) throw new ArgumentOutOfRangeException(nameof(index));
            int count = objs.Count;
            if (count > 0) {
                EnsureCapacity(ref _items, Size + count);
                if (index < Size)
                    Array.Copy(_items, index, _items, index + count, Size - index);

                if (_items == objs) {
                    // Copy first part of _items to insert location
                    Array.Copy(_items, 0, _items, index, index);
                    // Copy last part of _items back to inserted location
                    Array.Copy(_items, index + count, _items, index * 2, Size - index);
                } else {
                    objs.CopyTo(_items, index);
                }

                Size += count;
            }
        }

        public void Insert(int index, T item) {
            if ((uint) index > (uint) this.Size)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (this.Size == this._items.Length)
                this.EnsureCapacity(this.Size + 1);
            if (index < this.Size)
                Array.Copy((Array) this._items, index, (Array) this._items, index + 1, this.Size - index);
            this._items[index] = item;
            ++this.Size;
            ++this._version;
        }

        public void EnsureCapacity(int min) {
            if (_items.Length >= min)
                return;
            int num = _items.Length == 0 ? 4 : _items.Length * 2;
            if ((uint) num > 2146435071U)
                num = 2146435071;
            if (num < min)
                num = min;

            if (num != _items.Length) {
                if (num > 0) {
                    T[] newItems = new T[num];
                    if (_items.Length > 0) {
                        Array.Copy(_items, newItems, _items.Length);
                    }

                    _items = newItems;
                } else {
                    _items = Array.Empty<T>();
                }
            }
        }

        public static void EnsureCapacity(ref T[] _items, int min) {
            if (_items.Length >= min)
                return;
            int num = _items.Length == 0 ? 4 : _items.Length * 2;
            if ((uint) num > 2146435071U)
                num = 2146435071;
            if (num < min)
                num = min;

            if (num != _items.Length) {
                if (num > 0) {
                    T[] newItems = new T[num];
                    if (_items.Length > 0) {
                        Array.Copy(_items, newItems, _items.Length);
                    }

                    _items = newItems;
                } else {
                    _items = Array.Empty<T>();
                }
            }
        }
    }
}