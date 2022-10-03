using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nucs.Reflection {
    /// <summary>
    /// Holds a reference to a value.
    /// </summary>
    /// <typeparam name="T">The type of the value that the <see cref = "StrongBox{T}"></see> references.</typeparam>
    public sealed class StrongBox<T> : System.Runtime.CompilerServices.IStrongBox, IEquatable<StrongBox<T>> {
        /// <summary>
        /// Gets the strongly typed value associated with the <see cref = "StrongBox{T}"></see>
        /// <remarks>This is explicitly exposed as a field instead of a property to enable loading the address of the field.</remarks>
        /// </summary>
        [MaybeNull] public T Value = default!;

        /// <summary>
        /// Initializes a new StrongBox which can receive a value when used in a reference call.
        /// </summary>
        public StrongBox() { }

        /// <summary>
        /// Initializes a new <see cref = "StrongBox{T}"></see> with the specified value.
        /// </summary>
        /// <param name="value">A value that the <see cref = "StrongBox{T}"></see> will reference.</param>
        public StrongBox(T value) {
            Value = value;
        }

        /// <summary>
        /// Initializes a new <see cref = "StrongBox{T}"></see> with the specified value.
        /// </summary>
        /// <param name="value">A value that the <see cref = "StrongBox{T}"></see> will reference.</param>
        public StrongBox(ref T value) {
            Value = value;
        }

        public void Deconstruct(out T value) {
            value = Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(StrongBox<T> box) =>
            box.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StrongBox<T>(T unboxed) =>
            new StrongBox<T>(unboxed);

        public override string ToString() {
            return Value?.ToString();
        }

        #region Equality members

        public bool Equals(StrongBox<T> other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StrongBox<T>) obj);
        }

        public override int GetHashCode() {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public static bool operator ==(StrongBox<T> left, StrongBox<T> right) {
            return Equals(left, right);
        }

        public static bool operator !=(StrongBox<T> left, StrongBox<T> right) {
            return !Equals(left, right);
        }

        #endregion

        object? System.Runtime.CompilerServices.IStrongBox.Value {
            get => Value;
            set => Value = (T) value!;
        }
    }
    /// <summary>
    /// Holds a reference to a value.
    /// </summary>
    /// <typeparam name="T">The type of the value that the <see cref = "StrongBox{T}"></see> references.</typeparam>
    public sealed class StrongBoxChain<T> : System.Runtime.CompilerServices.IStrongBox, IEquatable<StrongBox<T>> {
        /// <summary>
        /// Gets the strongly typed value associated with the <see cref = "StrongBox{T}"></see>
        /// <remarks>This is explicitly exposed as a field instead of a property to enable loading the address of the field.</remarks>
        /// </summary>
        [MaybeNull] public T Value = default!;
        /// <summary>
        /// Gets the strongly typed value associated with the <see cref = "StrongBox{T}"></see>
        /// <remarks>This is explicitly exposed as a field instead of a property to enable loading the address of the field.</remarks>
        /// </summary>
        [MaybeNull] public StrongBoxChain<T>? Next = default!;

        /// <summary>
        /// Initializes a new StrongBox which can receive a value when used in a reference call.
        /// </summary>
        public StrongBoxChain() { }

        /// <summary>
        /// Initializes a new <see cref = "StrongBox{T}"></see> with the specified value.
        /// </summary>
        /// <param name="value">A value that the <see cref = "StrongBox{T}"></see> will reference.</param>
        public StrongBoxChain(T value) {
            Value = value;
        }

        /// <summary>
        /// Initializes a new <see cref = "StrongBox{T}"></see> with the specified value.
        /// </summary>
        /// <param name="value">A value that the <see cref = "StrongBox{T}"></see> will reference.</param>
        public StrongBoxChain(ref T value) {
            Value = value;
        }

        public void Deconstruct(out T value) {
            value = Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T(StrongBoxChain<T> box) =>
            box.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StrongBoxChain<T>(T unboxed) =>
            new StrongBoxChain<T>(unboxed);

        public override string ToString() {
            return Value?.ToString();
        }

        #region Equality members

        public bool Equals(StrongBox<T> other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StrongBox<T>) obj);
        }

        public override int GetHashCode() {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public static bool operator ==(StrongBoxChain<T> left, StrongBoxChain<T> right) {
            return Equals(left, right);
        }

        public static bool operator !=(StrongBoxChain<T> left, StrongBoxChain<T> right) {
            return !Equals(left, right);
        }

        #endregion

        object? System.Runtime.CompilerServices.IStrongBox.Value {
            get => Value;
            set => Value = (T) value!;
        }
    }
}