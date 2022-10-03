using System;

namespace Nucs.Disposable {
    public sealed class DisposableWrapper : IDisposable {
        private Action? DisposeCallback;

        public DisposableWrapper(Action disposeCallback) {
            DisposeCallback = disposeCallback ?? throw new ArgumentNullException(nameof(disposeCallback));
        }

        public void Dispose() {
            DisposeCallback?.Invoke();
            DisposeCallback = null!;
        }
    }
}