using System;
using System.Runtime.CompilerServices;
using Nucs.Collections.Structs;

namespace Nucs.Events {
    public readonly struct EventToken<TArgs> {
        public readonly int Token;

        public EventToken(int token) {
            Token = token;
        }

        public EventReference<TArgs> GetReference() =>
            new EventReference<TArgs>(Token);
    }

    public readonly struct EventReference<TArgs> : IDisposable {
        public readonly int Token;

        public EventReference(int token) {
            Token = token;
        }

        #region IDisposable

        public void Dispose() {
            if (Token != 0)
                Events.Unsubscribe<TArgs>(Token);
        }

        #endregion
    }

    public delegate void EventDelegate<in TPayload>(object sender, TPayload payload);

    public sealed class Delegate<TPayload> : IDelegate {
        private StructList<(int Token, EventDelegate<TPayload> Event)> DelegateList = new(0);
        public Type Type => typeof(TPayload);

        private readonly object _listChangingLock = new();

        private int _tokenIncrementor;

        private volatile bool _pendingCleanup;
        private int _nullReferences;

        public bool IsEmpty => DelegateList._count == 0;
        public int Count => DelegateList._count;

        public int Subscribe(EventDelegate<TPayload> act) {
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

            ((int Token, EventDelegate<TPayload> Event)[] array, int count) = DelegateList;
            for (int i = 0; i < count; i++) {
                array[i].Event?.Invoke(sender, payload);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invoke(object sender, TPayload payload) {
            if (_pendingCleanup) Cleanup();

            ((int Token, EventDelegate<TPayload> Event)[] array, int count) = DelegateList;
            for (int i = 0; i < count; i++) {
                array[i].Event?.Invoke(sender, payload);
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
            return Subscribe((EventDelegate<TPayload>) act);
        }

        bool IDelegate.Unsubscribe(int token) {
            return Unsubscribe(token);
        }

        void IDelegate.Invoke<T>(object sender, T payload) {
            #if DEBUG
            if (payload is not null && payload is not TPayload && !typeof(TPayload).IsValueType)
                throw new InvalidCastException($"Can't cast {typeof(T).Name} to {payload?.GetType().Name}");
            #endif

            Invoke(sender, Unsafe.As<T, TPayload>(ref payload));
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