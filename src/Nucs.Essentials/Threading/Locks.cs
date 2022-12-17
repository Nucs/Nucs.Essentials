using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Nucs {
    /// <summary>
    ///     A staticly shared lock
    /// </summary>
    public static class Locks {
        /// <summary>
        ///     A lock for controlling UI
        /// </summary>
        public static readonly SemaphoreSlim Single = new SemaphoreSlim(1, 1);

        /// <summary>
        ///     A shared lock for every symbol
        /// </summary>
        public static readonly StringCacheTable<SemaphoreSlim> Named = new StringCacheTable<SemaphoreSlim>(SemaphoreFactory);

        private static SemaphoreSlim SemaphoreFactory(string s) {
            return new SemaphoreSlim(1, 1);
        }

        public class StringCacheTable<T> {
            private readonly Func<string, T> _factory;

            public Func<string, T> Factory => _factory;

            private readonly ConcurrentDictionary<string, T> _instances = new ConcurrentDictionary<string, T>();

            public T this[string symbol] => _instances.GetOrAdd(symbol, _factory);

            public bool Contains(string symbol) =>
                _instances.TryGetValue(symbol, out _);

            public StringCacheTable(Func<string, T> factory) {
                _factory = factory;
            }
        }
    }
}