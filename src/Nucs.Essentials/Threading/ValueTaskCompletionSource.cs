using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nucs.Threading {
    /// <summary>
    /// Represents the producer side of of a <see cref="ValueTask{TResult}"/>
    /// providing access to the consumer side with through the <see cref="Task"/> property.
    /// </summary>
    /// <typeparam name="T">The type of result value.</typeparam>
    public readonly struct ValueTaskCompletionSource<T> : IEquatable<ValueTaskCompletionSource<T>> {
        private readonly ValueTaskSource<T> _source;
        private readonly short _token;

        private ValueTaskCompletionSource(ValueTaskSource<T> source) {
            Debug.Assert(source != null);
            Debug.Assert(!source!.Exhausted);

            var token = source.Token;

            _source = source;
            _token = token;
            Task = new ValueTask<T>(source, token);
        }

        /// <summary>
        /// Gets a <see cref="ValueTask{TResult}"/> created by the <see cref="ValueTaskCompletionSource{T}"/>.
        /// </summary>
        public ValueTask<T> Task { get; }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetCanceled() {
            return TrySetCanceled(cancellation: default);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <param name="cancellation">A <see cref="CancellationToken"/>.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetCanceled(CancellationToken cancellation) {
            return _source?.TryNotifyCompletion(cancellation, _token) ?? false;
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> that caused the task to fail.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetException(Exception exception) {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return _source?.TryNotifyCompletion(exception, _token) ?? false;
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exceptions">The collection of<see cref="Exception"/>s that caused the task to fail.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetException(IEnumerable<Exception> exceptions) {
            if (exceptions == null)
                throw new ArgumentNullException(nameof(exceptions));

            var exception = exceptions.FirstOrDefault();

            if (exception == null) {
                if (!exceptions.Any())
                    throw new ArgumentException("The collection must not be empty.", nameof(exceptions));

                throw new ArgumentException("The collection must not contain null entries.", nameof(exceptions));
            }

            return TrySetException(exception);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/>
        /// to the <c>CompletedSuccessfully</c> state.
        /// </summary>
        /// <param name="result">The result value to bind to the <see cref="ValueTask{TResult}"/>.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetResult(T result) {
            return _source?.TryNotifyCompletion(result, _token) ?? false;
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
        public void SetCanceled() {
            if (!TrySetCanceled()) {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <param name="cancellation">The <see cref="CancellationToken"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
        public void SetCanceled(CancellationToken cancellation) {
            if (!TrySetCanceled(cancellation)) {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> that caused the task to fail.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
        public void SetException(Exception exception) {
            if (!TrySetException(exception)) {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exceptions">The collection of<see cref="Exception"/>s that caused the task to fail.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
        public void SetException(IEnumerable<Exception> exceptions) {
            if (!TrySetException(exceptions)) {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>CompletedSuccessfully</c> state.
        /// </summary>
        /// <param name="result">The result value to bind to the <see cref="ValueTask{TResult}"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
        public void SetResult(T result) {
            if (!TrySetResult(result)) {
                ThrowAlreadyCompleted();
            }
        }

        private static void ThrowAlreadyCompleted() {
            throw new InvalidOperationException("An attempt was made to transition a value task to a final state when it had already completed");
        }

        public static ValueTaskCompletionSource<T> Create() {
            var source = ValueTaskSource<T>.Allocate();
            return new ValueTaskCompletionSource<T>(source);
        }

        /// <inheritdoc/>
        public bool Equals(ValueTaskCompletionSource<T> other) {
            return _source == other._source && _token == other._token;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) {
            return obj is ValueTaskCompletionSource<T> valueTaskCompletionSource && Equals(valueTaskCompletionSource);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return (_source, _token).GetHashCode();
        }

        /// <summary>
        /// Gets a boolean value indicating whether two <see cref="ValueTaskCompletionSource{T}"/> are equal.
        /// </summary>
        /// <param name="left">The first <see cref="ValueTaskCompletionSource{T}"/>.</param>
        /// <param name="right">The second <see cref="ValueTaskCompletionSource{T}"/>.</param>
        /// <returns>True if <paramref name="left"/> equals <paramref name="right"/>, false otherwise.</returns>
        public static bool operator ==(in ValueTaskCompletionSource<T> left, in ValueTaskCompletionSource<T> right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Gets a boolean value indicating whether two <see cref="ValueTaskCompletionSource{T}"/> are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="ValueTaskCompletionSource{T}"/>.</param>
        /// <param name="right">The second <see cref="ValueTaskCompletionSource{T}"/>.</param>
        /// <returns>True if <paramref name="left"/> does not equal <paramref name="right"/>, false otherwise.</returns>
        public static bool operator !=(in ValueTaskCompletionSource<T> left, in ValueTaskCompletionSource<T> right) {
            return !left.Equals(right);
        }

        public static async ValueTask<T[]> WhenAll<T>(params ValueTask<T>[] tasks) {
            // We don't allocate the list if no task throws
            List<Exception>? exceptions = null;

            var results = new T[tasks.Length];
            for (var i = 0; i < tasks.Length; i++)
                try {
                    results[i] = await tasks[i].ConfigureAwait(false);
                } catch (Exception ex) {
                    exceptions ??= new List<Exception>(tasks.Length);
                    exceptions.Add(ex);
                }

            return exceptions is null
                ? results
                : throw new AggregateException(exceptions);
        }
    }
}