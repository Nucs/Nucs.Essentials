using System;
using System.Collections.Generic;

namespace Nucs.Timing {
    public class FunctionalComparer<T> : IComparer<T> {
        private readonly Func<T, T, int> _comparer;

        #region Implementation of IComparer<in T>

        public FunctionalComparer(Func<T, T, int> comparer) {
            _comparer = comparer;
        }

        public int Compare(T x, T y) {
            return _comparer(x, y);
        }

        #endregion
    }
}