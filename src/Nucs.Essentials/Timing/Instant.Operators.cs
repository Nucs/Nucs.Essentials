using System;

namespace Nucs.Timing {
    public readonly partial struct Instant {
        #region Relational members

        public static implicit operator Instant(long ticks) {
            return new Instant(ticks);
        }

        public static implicit operator Instant(DateTime ticks) {
            return new Instant(ticks.Ticks);
        }

        public static implicit operator Instant(TimeSpan ticks) {
            return new Instant(ticks.Ticks);
        }

        public static explicit operator long(Instant instant) {
            return instant.Ticks;
        }

        public static explicit operator DateTime(Instant instant) {
            return new DateTime(instant.Ticks);
        }

        public static explicit operator TimeSpan(Instant instant) {
            return new TimeSpan(instant.Ticks);
        }

        public static bool operator <(Instant left, Instant right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Instant left, Instant right) {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Instant left, Instant right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Instant left, Instant right) {
            return left.CompareTo(right) >= 0;
        }

        #endregion

        #region Equality members

        public bool Equals(Instant other) {
            return Ticks == other.Ticks;
        }

        public bool Equals(TimeSpan other) {
            return Ticks == other.Ticks;
        }

        public bool Equals(DateTime other) {
            return Ticks == other.Ticks;
        }

        public override bool Equals(object obj) {
            return obj is Instant other && Equals(other);
        }

        public static bool operator ==(Instant left, Instant right) {
            return left.Ticks == right.Ticks;
        }

        public static bool operator !=(Instant left, Instant right) {
            return left.Ticks != right.Ticks;
        }

        public static bool operator ==(Instant left, TimeSpan right) {
            return left.Ticks == right.Ticks;
        }

        public static bool operator !=(Instant left, TimeSpan right) {
            return left.Ticks != right.Ticks;
        }

        public static bool operator ==(TimeSpan left, Instant right) {
            return left.Ticks == right.Ticks;
        }

        public static bool operator !=(TimeSpan left, Instant right) {
            return left.Ticks != right.Ticks;
        }

        public static bool operator ==(Instant left, long right) {
            return left.Ticks == right;
        }

        public static bool operator !=(Instant left, long right) {
            return left.Ticks != right;
        }

        public static bool operator ==(long left, Instant right) {
            return left == right.Ticks;
        }

        public static bool operator !=(long left, Instant right) {
            return left != right.Ticks;
        }

        public static bool operator ==(Instant left, DateTime right) {
            return left.Ticks == right.Ticks;
        }

        public static bool operator !=(Instant left, DateTime right) {
            return left.Ticks != right.Ticks;
        }

        public static bool operator ==(DateTime left, Instant right) {
            return left.Ticks == right.Ticks;
        }

        public static bool operator !=(DateTime left, Instant right) {
            return left.Ticks != right.Ticks;
        }

        #endregion


        public static Instant operator +(Instant instant, long ticks) {
            return new Instant(instant.Ticks + ticks);
        }

        public static Instant operator -(Instant instant, long ticks) {
            return new Instant(instant.Ticks - ticks);
        }

        public static Instant operator *(Instant instant, long ticks) {
            return new Instant(instant.Ticks * ticks);
        }

        public static Instant operator /(Instant instant, long ticks) {
            return new Instant(instant.Ticks / ticks);
        }

        public static Instant operator %(Instant instant, long ticks) {
            return new Instant(instant.Ticks % ticks);
        }

        public static Instant operator +(long ticks, Instant instant) {
            return new Instant(ticks + instant.Ticks);
        }

        public static Instant operator -(long ticks, Instant instant) {
            return new Instant(ticks - instant.Ticks);
        }

        public static Instant operator *(long ticks, Instant instant) {
            return new Instant(ticks * instant.Ticks);
        }

        public static Instant operator /(long ticks, Instant instant) {
            return new Instant(ticks / instant.Ticks);
        }

        public static Instant operator %(long ticks, Instant instant) {
            return new Instant(ticks % instant.Ticks);
        }

        public static Instant operator +(Instant instant, int ticks) {
            return new Instant(instant.Ticks + ticks);
        }

        public static Instant operator -(Instant instant, int ticks) {
            return new Instant(instant.Ticks - ticks);
        }

        public static Instant operator *(Instant instant, int ticks) {
            return new Instant(instant.Ticks * ticks);
        }

        public static Instant operator /(Instant instant, int ticks) {
            return new Instant(instant.Ticks / ticks);
        }

        public static Instant operator %(Instant instant, int ticks) {
            return new Instant(instant.Ticks % ticks);
        }

        public static Instant operator +(int ticks, Instant instant) {
            return new Instant(ticks + instant.Ticks);
        }

        public static Instant operator -(int ticks, Instant instant) {
            return new Instant(ticks - instant.Ticks);
        }

        public static Instant operator *(int ticks, Instant instant) {
            return new Instant(ticks * instant.Ticks);
        }

        public static Instant operator /(int ticks, Instant instant) {
            return new Instant(ticks / instant.Ticks);
        }

        public static Instant operator %(int ticks, Instant instant) {
            return new Instant(ticks % instant.Ticks);
        }

        public static Instant operator +(Instant ticks, Instant instant) {
            return new Instant(ticks.Ticks + instant.Ticks);
        }

        public static Instant operator -(Instant ticks, Instant instant) {
            return new Instant(ticks.Ticks - instant.Ticks);
        }

        public static Instant operator *(Instant ticks, Instant instant) {
            return new Instant(ticks.Ticks * instant.Ticks);
        }

        public static Instant operator /(Instant ticks, Instant instant) {
            return new Instant(ticks.Ticks / instant.Ticks);
        }

        public static Instant operator %(Instant ticks, Instant instant) {
            return new Instant(ticks.Ticks % instant.Ticks);
        }
    }
}