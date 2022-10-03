using System;
using System.Runtime.CompilerServices;
using Nucs.Collections.Structs;

namespace Nucs.Events {
    public delegate void RefEventDelegate<TPayload>(object sender, ref TPayload payload);

    public sealed class RefDelegate<TPayload> : IDelegate {
        private StructList<(int Token, RefEventDelegate<TPayload> Event)> DelegateList = new(0);
        public bool IsEmpty => DelegateList._count == 0;
        public Type Type => typeof(TPayload);

        private readonly object _listChangingLock = new();

        private int _tokenIncrementor;

        private volatile bool _pendingCleanup;

        private int _nullReferences;

        public int Subscribe(RefEventDelegate<TPayload> act) {
            lock (_listChangingLock) {
                var token = ++_tokenIncrementor;
                DelegateList.Add((token, act));
                return token;
            }
        }

        public bool Unsubscribe(int token) {
            lock (_listChangingLock) {
                var cnt = DelegateList._count;
                for (int i = cnt - 1; i >= 0; i--) {
                    if (DelegateList[i].Token == token) {
                        DelegateList[i] = default;
                        if (++_nullReferences > (int) (DelegateList._count * 0.2)) {
                            _pendingCleanup = true;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public void Clear() {
            lock (_listChangingLock)
                DelegateList.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(object sender, ref TPayload payload) {
            if (_pendingCleanup) Cleanup();

            ((int Token, RefEventDelegate<TPayload> Event)[] array, int count) = DelegateList;
            for (int i = 0; i < count; i++) {
                array[i].Event?.Invoke(sender, ref payload);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(object sender, TPayload payload) {
            if (_pendingCleanup) Cleanup();

            ((int Token, RefEventDelegate<TPayload> Event)[] array, int count) = DelegateList;
            for (int i = 0; i < count; i++) {
                array[i].Event?.Invoke(sender, ref payload);
            }
        }

        public void Cleanup() {
            lock (_listChangingLock) {
                if (!_pendingCleanup)
                    return;

                var cnt = DelegateList._count;
                for (int i = cnt - 1; i >= 0; i--) {
                    if (DelegateList[i].Token == 0)
                        DelegateList.RemoveAt(i);
                }

                _pendingCleanup = false;
            }
        }

        #region Implementation of IDelegate

        int IDelegate.Subscribe(Delegate act) {
            return Subscribe((RefEventDelegate<TPayload>) act);
        }

        bool IDelegate.Unsubscribe(int token) {
            return Unsubscribe(token);
        }

        void IDelegate.Invoke<T>(object sender, T payload) {
            #if DEBUG
            if (payload is not null && payload is not TPayload && !typeof(TPayload).IsValueType)
                throw new InvalidCastException($"Can't cast {typeof(T).Name} to {payload?.GetType().Name}");
            #endif

            Invoke(sender, ref Unsafe.As<T, TPayload>(ref payload));
        }

        void IDelegate.Invoke<T>(object sender, ref T payload) {
            #if DEBUG
            if (payload is not null && payload is not TPayload && !typeof(TPayload).IsValueType)
                throw new InvalidCastException($"Can't cast {typeof(T).Name} to {payload?.GetType().Name}");
            #endif

            Invoke(sender, ref Unsafe.As<T, TPayload>(ref payload));
        }

        #endregion
    }
}