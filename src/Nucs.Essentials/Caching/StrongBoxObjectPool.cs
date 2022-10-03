using System;
using System.Collections.Concurrent;
using System.Threading;
using Nucs.Reflection;

namespace Nucs.Caching {
    public interface IStrongBoxPool {
        void Clear();
        void ClearHalf();
        int Count { get; }
    }

    public interface IStrongBoxPool<T> : IStrongBoxPool {
        PooledStrongBox<T> Get(ref T value);
        PooledStrongBox<T> Get(T value);
        PooledStrongBox<T> Get();
    }

    public sealed class StrongBoxObjectPool<T> : IDisposable, IStrongBoxPool<T> {
        private readonly int? _maxCount;
        private readonly ConcurrentQueue<PooledStrongBox<T>> _bag = new ConcurrentQueue<PooledStrongBox<T>>();
        private readonly Timer Timer;

        public void Clear() {
            var count = _bag.Count;
            for (int i = 0; i < count && _bag.TryDequeue(out var obj); i++) {
                obj.isUsed = false; //won't be returned to pool
            }
        }

        public void ClearHalf() {
            var count = _bag.Count / 2;
            for (int i = 0; i < count && _bag.TryDequeue(out var obj); i++) {
                obj.isUsed = false; //won't be returned to pool
            }
        }

        public int Count => _bag.Count;

        public bool IsEmpty => _bag.IsEmpty;

        public StrongBoxObjectPool() : this(null, null) { }

        public StrongBoxObjectPool(int? maxCount, TimeSpan? cleanupInterval) {
            if ((_maxCount.HasValue || cleanupInterval.HasValue) && (_maxCount.HasValue && cleanupInterval.HasValue) == false)
                throw new ArgumentException("Must specify both maxCount and cleanupInterval");

            _maxCount = maxCount;
            if (cleanupInterval.HasValue && _maxCount.HasValue)
                Timer = new Timer(state => {
                    for (int i = _bag.Count - 1; i >= _maxCount.Value; i--) {
                        _bag.TryDequeue(out _);
                    }
                }, null, cleanupInterval.Value, cleanupInterval.Value);
        }

        public PooledStrongBox<T> Get(ref T value) {
            if (!_bag.TryDequeue(out var obj))
                obj = new PooledStrongBox<T>();

            obj.isUsed = true;
            obj.Value = value;
            return obj;
        }

        public PooledStrongBox<T> Get(T value) {
            if (!_bag.TryDequeue(out var obj))
                obj = new PooledStrongBox<T>();

            obj.isUsed = true;
            obj.Value = value;
            return obj;
        }

        public PooledStrongBox<T> Get() {
            if (!_bag.TryDequeue(out var obj))
                obj = new PooledStrongBox<T>();

            obj.Value = default;
            obj.isUsed = true;
            return obj;
        }

        public PooledStrongBox<T> GetNew() {
            if (!_bag.TryDequeue(out var obj))
                obj = new PooledStrongBox<T>(DefaultValue<T>.GetDefaultNew!);
            else
                obj.Value = DefaultValue<T>.GetDefaultNew!;

            obj.isUsed = true;
            return obj;
        }

        public PooledStrongBox<T> GetOrNew() {
            if (!_bag.TryDequeue(out var obj))
                obj = new PooledStrongBox<T>(DefaultValue<T>.GetDefaultNew!);

            obj.isUsed = true;
            return obj;
        }

        internal void Return(PooledStrongBox<T> obj) {
            _bag.Enqueue(obj);
        }

        #region IDisposable

        public void Dispose() {
            Timer?.Dispose();
            _bag.Clear();
        }

        #endregion
    }
}