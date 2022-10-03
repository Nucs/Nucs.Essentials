using System;

namespace Nucs.Disposable;

public sealed class StateDisposableWrapper<T> : IDisposable {
    private readonly T State;
    private Action<T>? DisposeCallback;

    public StateDisposableWrapper(T state, Action<T> disposeCallback) {
        State = state;
        DisposeCallback = disposeCallback ?? throw new ArgumentNullException(nameof(disposeCallback));
    }

    public void Dispose() {
        DisposeCallback?.Invoke(State);
        DisposeCallback = null!;
    }
}