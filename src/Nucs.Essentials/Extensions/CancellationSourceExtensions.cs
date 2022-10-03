using System;
using System.Threading;

namespace Nucs.Extensions {
    public static class CancellationSourceExtensions {
        /// <summary>
        ///     Cancels the <see cref="CancellationTokenSource"/> and then disposes it. Swallows any exception, filters null.
        /// </summary>
        /// <param name="src"></param>
        public static void SafeCancelAndDispose(this CancellationTokenSource src) {
            if (src != null) {
                try {
                    if (!src.IsCancellationRequested)
                        src.Cancel();
                    src.Dispose();
                } catch {
                    // ignored
                }
            }
        }

        /// <summary>
        ///     Cancels the <see cref="CancellationTokenSource"/>. Swallows any exception, filters null.
        /// </summary>
        /// <param name="src"></param>
        public static void TryCancel(this CancellationTokenSource src) {
            SafeCancel(src);
        }

        /// <summary>
        ///     Cancels the <see cref="CancellationTokenSource"/> and then disposes it. Swallows any exception, filters null.
        /// </summary>
        /// <param name="src"></param>
        public static void TryCancelAndDispose(this CancellationTokenSource src) {
            SafeCancelAndDispose(src);
        }

        /// <summary>
        ///     Cancels the <see cref="CancellationTokenSource"/>. Swallows any exception, filters null.
        /// </summary>
        /// <param name="src"></param>
        public static void SafeCancel(this CancellationTokenSource src) {
            if (src != null) {
                try {
                    if (!src.IsCancellationRequested)
                        src.Cancel();
                } catch {
                    // ignored
                }
            }
        }

        /// <summary>
        ///     Cancels the <see cref="IDisposable"/> and then disposes it. Swallows any exception, filters null.
        /// </summary>
        /// <param name="src"></param>
        public static void TryDispose(this object obj) {
            if (obj != null && obj is IDisposable src) {
                try {
                    src.Dispose();
                } catch {
                    // ignored
                }
            }
        }

        /// <summary>
        ///     Cancels the <see cref="IDisposable"/> and then disposes it. Swallows any exception, filters null.
        /// </summary>
        public static void TryDispose<T>(this T obj) where T : class, IDisposable {
            if (!Equals(obj, default(T))) {
                try {
                    obj.Dispose();
                } catch {
                    // ignored
                }
            }
        }

        /// <summary>
        ///     Cancels the <see cref="IDisposable"/> and then disposes it. Swallows any exception, filters null.
        /// </summary>
        /// <param name="src"></param>
        public static void SafeDispose(this IDisposable obj) {
            if (obj != null)
                try {
                    obj.Dispose();
                } catch {
                    // ignored
                }
        }
    }
}