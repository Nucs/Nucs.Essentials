using System;
using System.Collections.Generic;

namespace Tradepath {
    public class Reference : IEquatable<Reference> {
        public object Value;

        public Reference(object value) {
            Value = value;
        }

        public Reference() { }

        #region Equality members

        public bool Equals(Reference other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Value, other.Value);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Reference) obj);
        }

        public override int GetHashCode() {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        public static bool operator ==(Reference left, Reference right) {
            return Equals(left, right);
        }

        public static bool operator !=(Reference left, Reference right) {
            return !Equals(left, right);
        }

        #endregion
    }

    public class Reference<T> : IEquatable<Reference<T>> {
        public T Value;

        private Reference(T value) {
            Value = value;
        }

        public Reference() { }

        #region Equality members

        public bool Equals(Reference<T> other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Reference<T>) obj);
        }

        public override int GetHashCode() {
            return EqualityComparer<T>.Default.GetHashCode(Value);
        }

        public static bool operator ==(Reference<T> left, Reference<T> right) {
            return Equals(left, right);
        }

        public static bool operator !=(Reference<T> left, Reference<T> right) {
            return !Equals(left, right);
        }

        #endregion

        public static implicit operator T(Reference<T> h) {
            return h.Value;
        }

        public static implicit operator Reference<T>(T h) {
            return new Reference<T>(h);
        }
    }

    public readonly struct ReadonlyReference<T> : IEquatable<ReadonlyReference<T>> where T : struct {
        public readonly T Value;

        private ReadonlyReference(T value) {
            Value = value;
        }

        public ReadonlyReference(ref T value) {
            Value = value;
        }

        public ReadonlyReference() {
            Value = default; 
        }

        public bool Equals(ReadonlyReference<T> other) {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj) {
            return obj is ReadonlyReference<T> other && Equals(other);
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public static bool operator ==(ReadonlyReference<T> left, ReadonlyReference<T> right) {
            return left.Equals(right);
        }

        public static bool operator !=(ReadonlyReference<T> left, ReadonlyReference<T> right) {
            return !left.Equals(right);
        }
        
        public static implicit operator T(ReadonlyReference<T> h) {
            return h.Value;
        }

        public static implicit operator ReadonlyReference<T>(T h) {
            return new ReadonlyReference<T>(ref h);
        }
    }
}