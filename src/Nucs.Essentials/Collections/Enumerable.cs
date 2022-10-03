using System.Collections;
using System.Collections.Generic;

namespace Nucs.Collections {
    /// <summary>
    ///     Wrapper for enumerating easily.
    /// </summary>
    public readonly struct Enumerable<TEnumerator, TValue> : IEnumerable<TValue> where TEnumerator : struct, IEnumerator<TValue> {
        private readonly TEnumerator _enumerator;

        #region Implementation of IEnumerable

        public Enumerable(TEnumerator enumerator) {
            _enumerator = enumerator;
        }

        public TEnumerator GetEnumerator() {
            return _enumerator;
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
            return _enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _enumerator;
        }

        #endregion
    }
}