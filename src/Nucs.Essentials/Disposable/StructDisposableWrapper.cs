using System;

namespace Nucs.Disposable;

public readonly struct StructDisposableWrapper : IDisposable {
    public readonly Action DisposeCallback;

    public StructDisposableWrapper(Action disposeCallback) {
        DisposeCallback = disposeCallback ?? throw new ArgumentNullException(nameof(disposeCallback));
    }

    public void Dispose() {
        DisposeCallback();
    }
}