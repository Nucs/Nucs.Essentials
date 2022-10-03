using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Nucs.Caching;

namespace Nucs.Reflection {
    /// <summary>
    /// Holds a reference to a value.
    /// </summary>
    /// <typeparam name="T">The type of the value that the <see cref = "PooledStrongBox{T}"></see> references.</typeparam>
    public static class PooledStrongBox {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledStrongBox<T> Get<T>(ref T value) where T : struct {
            return PooledStrongBox<T>.Pool.Get(ref value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledStrongBox<T> Get<T>(T value) where T : struct {
            return PooledStrongBox<T>.Pool.Get(value);
        }

        internal static readonly List<IStrongBoxPool> _allocatedPools = new List<IStrongBoxPool>();

        internal static Timer _halvingTimer;

        internal static void InitializeHalving() {
            PooledStrongBox._halvingTimer ??= new Timer(PooledStrongBox.HalvenPools, _allocatedPools, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30));
        }

        private static void HalvenPools(object state) {
            var list = (List<IStrongBoxPool>) state;
            lock (list) {
                foreach (var pool in list) {
                    pool.ClearHalf();
                }
            }
        }
    }


    public interface IUnsafeStrongbox : System.Runtime.CompilerServices.IStrongBox {
        /// <summary>
        ///     Will unsafely return an instance of given type allowing quick unboxing and casting of T Value.
        /// </summary>
        public ref T UnboxAs<T>();

        /// <summary>
        ///     The type this Strongbox is wrapping/boxing.
        /// </summary>
        public Type BoxedType { get; }
        
        /// <summary>
        ///     Will create a new empty strongbox of the same type.
        /// </summary>
        public IUnsafeStrongbox CreateSimilarBox();

        /// <summary>
        ///     Will create a new empty strongbox of the same type initializing with <paramref name="value"/>.
        /// </summary>
        public IUnsafeStrongbox CreateSimilarBox<T>(T value);

        /// <summary>
        ///     Will create a new empty strongbox of the same type initializing with <paramref name="value"/>.
        /// </summary>
        public IUnsafeStrongbox CreateSimilarBox<T>(ref T value);

        /// <summary>
        ///     Will create a new empty strongbox of the same type
        ///     initializing with DefaultValue.DefaultNew which calls new T() where T is this strongbox's type
        /// </summary>
        public IUnsafeStrongbox CreateSimilarBoxDefaultNew();
    }

    public sealed class PooledStrongBox<T> : IUnsafeStrongbox, IEquatable<PooledStrongBox<T>>, IDisposable {
        public static readonly StrongBoxObjectPool<T> Pool;

        static PooledStrongBox() {
            Pool = new StrongBoxObjectPool<T>(null, null);
            PooledStrongBox.InitializeHalving();
            lock (PooledStrongBox._allocatedPools) {
                PooledStrongBox._allocatedPools.Add(Pool);
            }
        }

        public static void ClearPool() {
            Pool.Clear();
        }

        public static PooledStrongBox<T> Get(ref T value) {
            return Pool.Get(ref value);
        }

        public static PooledStrongBox<T> Get(T value) {
            return Pool.Get(value);
        }


        public static PooledStrongBox<T> Get() {
            return Pool.Get();
        }

        public static PooledStrongBox<T> GetNew() {
            return Pool.GetNew();
        }

        #region IDisposable

        public void Dispose() {
            if (!isUsed)
                return;
            isUsed = false;
            Pool.Return(this);
        }

        ~PooledStrongBox() {
            if (!isUsed)
                return;
            isUsed = false;
            Pool.Return(this);
        }

        #endregion

        /// <summary>
        /// Gets the strongly typed value associated with the <see cref = "PooledStrongBox{T}"></see>
        /// <remarks>This is explicitly exposed as a field instead of a property to enable loading the address of the field.</remarks>
        /// </summary>
        [MaybeNull] public T Value = default!;

        internal volatile bool isUsed = true;

        /// <summary>
        /// Initializes a new PooledStrongBox which can receive a value when used in a reference call.
        /// </summary>
        public PooledStrongBox() { }

        /// <summary>
        /// Initializes a new <see cref = "PooledStrongBox{T}"></see> with the specified value.
        /// </summary>
        /// <param name="value">A value that the <see cref = "PooledStrongBox{T}"></see> will reference.</param>
        public PooledStrongBox(T value) {
            Value = value;
        }

        /// <summary>
        /// Initializes a new <see cref = "PooledStrongBox{T}"></see> with the specified value.
        /// </summary>
        /// <param name="value">A value that the <see cref = "PooledStrongBox{T}"></see> will reference.</param>
        public PooledStrongBox(ref T value) {
            Value = value;
        }

        public void Deconstruct(out T value) {
            value = Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(PooledStrongBox<T> box) =>
            box.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator PooledStrongBox<T>(T unboxed) {
            var pool = Pool.Get();
            pool.Value = unboxed;
            return pool;
        }

        private static readonly bool _isValueType = typeof(T).IsValueType;

        public override string ToString() {
            if (_isValueType)
                return StructToString<T>.ToString(ref Value);
            return Value.ToString();
        }

        #region Equality members

        public bool Equals(PooledStrongBox<T> other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PooledStrongBox<T>) obj);
        }

        public override int GetHashCode() {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public static bool operator ==(PooledStrongBox<T> left, PooledStrongBox<T> right) {
            return Equals(left, right);
        }

        public static bool operator !=(PooledStrongBox<T> left, PooledStrongBox<T> right) {
            return !Equals(left, right);
        }

        #endregion

        object? System.Runtime.CompilerServices.IStrongBox.Value {
            get => Value;
            set => Value = (T) value!;
        }

        /// <summary>
        ///     Will unsafely return an instance of given type allowing quick unboxing and casting of T Value.
        /// </summary>
        public ref T1 UnboxAs<T1>() {
            return ref Unsafe.As<T, T1>(ref Value!);
        }

        public Type BoxedType => typeof(T);

        public IUnsafeStrongbox CreateSimilarBox() {
            return Pool.Get();
        }

        public IUnsafeStrongbox CreateSimilarBox(object value) {
            return Pool.Get((T) value);
        }

        public IUnsafeStrongbox CreateSimilarBox<T1>(T1 value) {
            return Pool.Get(ref Unsafe.As<T1, T>(ref value!));
        }

        public IUnsafeStrongbox CreateSimilarBox<T1>(ref T1 value) {
            return Pool.Get(ref Unsafe.As<T1, T>(ref value!));
        }

        public IUnsafeStrongbox CreateSimilarBoxDefaultNew() {
            return Pool.Get(DefaultValue<T>.GetDefaultNew!);
        }
    }
}