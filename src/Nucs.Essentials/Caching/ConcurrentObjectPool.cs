using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Nucs.Caching {
    public sealed class ConcurrentObjectPool<T> : IDisposable where T : class, new() {
        private readonly int? _maxCount;
        private readonly ConcurrentBag<T> _bag = new ConcurrentBag<T>();
        private readonly Timer Timer;

        public ConcurrentObjectPool() { }

        public ConcurrentObjectPool(int? maxCount, TimeSpan? cleanupInterval) {
            if ((_maxCount.HasValue || cleanupInterval.HasValue) && (_maxCount.HasValue && cleanupInterval.HasValue) == false)
                throw new ArgumentException("Must specify both maxCount and cleanupInterval");

            _maxCount = maxCount;
            if (cleanupInterval.HasValue && _maxCount.HasValue)
                Timer = new Timer(state => {
                    for (int i = _bag.Count - 1; i >= _maxCount.Value; i--) {
                        _bag.TryTake(out _);
                    }
                }, null, cleanupInterval.Value, cleanupInterval.Value);
        }

        public T Get() {
            return _bag.TryTake(out var obj) ? obj : new T(); //create new which will then be added back
        }

        public void Return(T obj) {
            _bag.Add(obj);
        }

        public void Return(ref T obj) {
            _bag.Add(obj);
        }

        #region IDisposable

        public void Dispose() {
            Timer?.Dispose();
            _bag.Clear();
        }

        #endregion
    }
}