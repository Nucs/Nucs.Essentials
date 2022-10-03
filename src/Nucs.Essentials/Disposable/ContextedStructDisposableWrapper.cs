using System;

namespace Nucs.Disposable;

public readonly struct ContextedStructDisposableWrapper<T> : IDisposable {
    public readonly Action<T> DisposeCallback;
    public readonly T DisposeContext;

    public ContextedStructDisposableWrapper(Action<T> disposeCallback, T disposeContext) {
        DisposeCallback = disposeCallback ?? throw new ArgumentNullException(nameof(disposeCallback));
        DisposeContext = disposeContext;
    }

    public void Dispose() {
        DisposeCallback(DisposeContext);
    }
}