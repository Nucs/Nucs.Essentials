using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Nucs.Threading {
    /// <summary>
    ///     Blocks all threads who called <see cref="Callback()"/> till any thread calls <see cref="Set()"/>.
    /// </summary>
    /// <example><br></br>
    /// var cb = new CallbackBlock();<br></br>
    ///
    /// //other thread:<br></br>
    /// var action = cb.Callback();<br></br>
    ///
    /// //somewhere else<br></br>
    /// cb.Add(()=>Console.WriteLine("hi"));<br></br>
    /// cb.Dispose();
    /// </example>
    public class CallbackBlock : IDisposable {
        private ManualResetEventSlim _reset { get; } = new ManualResetEventSlim();

        internal BlockingCollection<Action> _actions { get; } = new BlockingCollection<Action>();

        /// <summary>
        ///     Wait for a callback method to be <see cref="Set"/>.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Action Callback(CancellationToken cancellationToken) {
            try {
                return _actions.Take(cancellationToken);
            } catch (ArgumentNullException) {
                return null;
            } catch (ObjectDisposedException) {
                return null;
            } catch (InvalidOperationException) {
                return null;
            }
        }

        /// <summary>
        ///     Wait for a callback method to be <see cref="Set"/>.
        /// </summary>
        /// <returns></returns>
        public Action Callback() {
            return Callback(CancellationToken.None);
        }

        /// <summary>
        ///     Releases all threads with this callback.
        /// </summary>
        /// <param name="callback"></param>
        public void Add(Action callback) {
            _actions.Add(callback);
            _reset.Set();
            _reset.Reset();
        }

        private bool _disposed = false;

        /// <inheritdoc />
        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;
            _actions.CompleteAdding();
            _actions.Dispose();
            _reset.Set();
            _reset?.Dispose();
        }
    }
}