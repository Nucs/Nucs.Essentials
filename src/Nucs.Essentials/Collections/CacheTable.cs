using System;
using System.Collections.Concurrent;

namespace Nucs.Collections {
    public class CacheTable<TKey, TValue> {
        private readonly Func<TKey, TValue> _factory;
        private readonly ConcurrentDictionary<TKey, TValue> _instances = new ConcurrentDictionary<TKey, TValue>();

        public Func<TKey, TValue> Factory => _factory;

        public TValue this[TKey symbol] {
            get => _instances.GetOrAdd(symbol, _factory);
            set => _instances[symbol] = value;
        }

        public bool Contains(TKey symbol) =>
            _instances.TryGetValue(symbol, out _);

        public CacheTable(Func<TKey, TValue> factory) {
            _factory = factory;
        }

        public void Clear() {
            _instances.Clear();
        }

        public void Remove(TKey key) {
            _instances.TryRemove(key, out _);
        }
    }
}