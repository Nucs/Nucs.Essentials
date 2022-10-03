using System;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using Nucs.Collections;

namespace Nucs.Caching {
    //TODO: this class is unstable,
    public class ExpandingObjectPool<T> : ObjectPool<T> where T : class {
        private readonly AdvancedList<T> _items;
        private readonly IPooledObjectPolicy<T> _policy;
        private byte _put;
        private byte _take;

        public ExpandingObjectPool(IPooledObjectPolicy<T> policy)
            : this(policy, Environment.ProcessorCount * 2) { }

        public ExpandingObjectPool(IPooledObjectPolicy<T> policy, int initialCapacity) {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _items = new AdvancedList<T>(initialCapacity);
        }

        public override T Get() {
            if (_items.Count == 0) {
                return _items.AddInline(_policy.Create());
            }

            if (unchecked(++_take) % 2 == 0) {
                ref T comparand = ref _items[0];
                if (comparand != null && Interlocked.CompareExchange(ref comparand, default, comparand) == comparand)
                    return comparand;

                for (int index = 1; index < _items.Count; ++index) {
                    comparand = ref _items[index];
                    if (comparand != null && Interlocked.CompareExchange(ref comparand, default, comparand) == comparand)
                        return comparand;
                }

                return _items.AddInline(_policy.Create());
            } else {
                ref T comparand = ref _items[_items.Count - 1];
                if (comparand != null && Interlocked.CompareExchange(ref comparand, default, comparand) == comparand)
                    return comparand;

                for (int index = _items.Count - 2; index >= 0; index--) {
                    comparand = ref _items[index];
                    if (comparand != null && Interlocked.CompareExchange(ref comparand, default, comparand) == comparand)
                        return comparand;
                }

                return _items.AddInline(_policy.Create());
            }
        }

        public override void Return(T obj) {
            if (!_policy.Return(obj))
                return;
            if (unchecked(++_put) % 2 == 0) {
                for (int index = 0; index < _items.Count; ++index) {
                    if (_items[index] == null) {
                        _items[index] = obj;
                        break;
                    }
                }
            } else {
                for (int index = _items.Count - 1; index >= 0; index--) {
                    if (_items[index] == null) {
                        _items[index] = obj;
                        break;
                    }
                }
            }
        }
    }
}