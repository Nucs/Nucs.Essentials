using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Exceptions;

namespace Nucs {
    [DebuggerStepThrough]
    public static class Retry {
        public static void Do(Action action, TimeSpan retryInterval, int maxAttemptCount = 3, bool @throw = true, Action<int, Exception> onError = null) {
            Do<object>(() => {
                action();
                return null;
            }, retryInterval, maxAttemptCount, @throw, onError);
        }

        public static T Do<T>(Func<T> action, TimeSpan retryInterval, int maxAttemptCount = 3, bool @throw = true, Action<int, Exception> onError = null) {
            Exception exception = null;
            var attempted = 0;
            if (maxAttemptCount == -1)
                while (true) {
                    try {
                        return action();
                    } catch (AbortRetryException e) {
                        SystemHelper.Logger?.Trace($"Retrying aborted...", e);
                        return default;
                    } catch (AbortRetryAndThrowException e) {
                        SystemHelper.Logger?.Error($"Retrying abroted and throwing exception at the {attempted} time...", e);
                        throw;
                    } catch (Exception ex) {
                        Thread.Sleep(retryInterval);
                        SystemHelper.Logger?.Error($"Retrying for the {++attempted} time...", ex);
                        onError?.Invoke(attempted, ex);
                        exception = ex;
                    }
                }
            else {
                for (; attempted < maxAttemptCount; attempted++)
                    try {
                        if (attempted > 0) Thread.Sleep(retryInterval);

                        return action();
                    } catch (AbortRetryException e) {
                        SystemHelper.Logger?.Trace($"Retrying aborted...", e);
                        return default;
                    } catch (AbortRetryAndThrowException e) {
                        SystemHelper.Logger?.Error($"Retrying abroted and throwing exception at the {attempted} time...", e);
                        throw;
                    } catch (Exception ex) {
                        Thread.Sleep(retryInterval);
                        SystemHelper.Logger?.Error($"Retrying for the {++attempted} time...", ex);
                        onError?.Invoke(attempted, ex);
                        exception = ex;
                    }

                //throw an exception with current stack by recapturing.
                try {
                    throw new RetryException(exception);
                } catch (RetryException ex) {
                    //catch inorder to record stacktrace
                    SystemHelper.Logger?.Error($"Retried unsuccessfully for the {attempted} times...", ex);
                    if (@throw) {
                        throw;
                    }

                    return default;
                }
            }
        }

        public static Task DoAsync(Func<Task> action, TimeSpan retryInterval, int maxAttemptCount = 3, bool @throw = true, Action<int, Exception> onError = null, bool configureAwait = true) {
            return DoAsync<object>(async () => {
                await action().ConfigureAwait(false);
                return default;
            }, retryInterval, maxAttemptCount, @throw, onError);
        }

        public static async Task<T> DoAsync<T>(Func<Task<T>> action, TimeSpan retryInterval, int maxAttemptCount = 3, bool @throw = true, Action<int, Exception> onError = null, bool configureAwait = true) {
            Exception exception = null;
            var attempted = 0;
            if (maxAttemptCount == -1)
                while (true) {
                    try {
                        return await action().ConfigureAwait(configureAwait);
                    } catch (AbortRetryException e) {
                        SystemHelper.Logger?.Trace($"Retrying aborted...", e);
                        return default;
                    } catch (AbortRetryAndThrowException e) {
                        SystemHelper.Logger?.Error($"Retrying abroted and throwing exception at the {attempted} time...", e);
                        throw;
                    } catch (Exception ex) {
                        Thread.Sleep(retryInterval);
                        SystemHelper.Logger?.Error($"Retrying for the {++attempted} time...", ex);
                        onError?.Invoke(attempted, ex);
                    }
                }
            else {
                for (; attempted < maxAttemptCount; attempted++)
                    try {
                        if (attempted > 0) Thread.Sleep(retryInterval);

                        return await action();
                    } catch (AbortRetryException e) {
                        SystemHelper.Logger?.Trace($"Retrying aborted...", e);
                        return default;
                    } catch (AbortRetryAndThrowException e) {
                        SystemHelper.Logger?.Error($"Retrying abroted and throwing exception at the {attempted} time...", e);
                        throw;
                    } catch (Exception ex) {
                        Thread.Sleep(retryInterval);
                        SystemHelper.Logger?.Error($"Retrying for the {++attempted} time...", ex);
                        onError?.Invoke(attempted, ex);
                        exception = ex;
                    }

                //throw an exception with current stack by recapturing.
                try {
                    throw new RetryException(exception);
                } catch (RetryException ex) {
                    //catch inorder to record stacktrace
                    SystemHelper.Logger?.Error($"Retried unsuccessfully for the {attempted} times...", ex);
                    if (@throw) {
                        throw;
                    }

                    return default;
                }
            }
        }

        public static async ValueTask DoAsync(Func<ValueTask> action, TimeSpan retryInterval, int maxAttemptCount = 3, bool @throw = true, Action<int, Exception> onError = null, bool configureAwait = true) {
            Exception exception = null;
            var attempted = 0;
            if (maxAttemptCount == -1)
                while (true) {
                    try {
                        await action().ConfigureAwait(configureAwait);
                        return;
                    } catch (AbortRetryException e) {
                        SystemHelper.Logger?.Trace($"Retrying aborted...", e);
                        return;
                    } catch (AbortRetryAndThrowException e) {
                        SystemHelper.Logger?.Error($"Retrying abroted and throwing exception at the {attempted} time...", e);
                        throw;
                    } catch (Exception ex) {
                        Thread.Sleep(retryInterval);
                        SystemHelper.Logger?.Error($"Retrying for the {++attempted} time...", ex);
                        onError?.Invoke(attempted, ex);
                        exception = ex;
                    }
                }
            else {
                for (; attempted < maxAttemptCount; attempted++)
                    try {
                        if (attempted > 0) Thread.Sleep(retryInterval);

                        await action();
                        return;
                    } catch (AbortRetryException e) {
                        SystemHelper.Logger?.Trace($"Retrying aborted...", e);
                        return;
                    } catch (AbortRetryAndThrowException e) {
                        SystemHelper.Logger?.Error($"Retrying abroted and throwing exception at the {attempted} time...", e);
                        throw;
                    } catch (Exception ex) {
                        Thread.Sleep(retryInterval);
                        SystemHelper.Logger?.Error($"Retrying for the {++attempted} time...", ex);
                        onError?.Invoke(attempted, ex);
                        exception = ex;
                    }

                //throw an exception with current stack by recapturing.
                try {
                    throw new RetryException(exception);
                } catch (RetryException ex) {
                    //catch inorder to record stacktrace
                    SystemHelper.Logger?.Error($"Retried unsuccessfully for the {attempted} times...", ex);
                    if (@throw) {
                        throw;
                    }

                    return;
                }
            }
        }

        public static async ValueTask<T> DoAsyncValueTask<T>(Func<ValueTask<T>> action, TimeSpan retryInterval, int maxAttemptCount = 3, bool @throw = true, Action<int, Exception> onError = null, bool configureAwait = true) {
            Exception exception = null;
            var attempted = 0;
            if (maxAttemptCount == -1)
                while (true) {
                    try {
                        return await action().ConfigureAwait(configureAwait);
                    } catch (AbortRetryException e) {
                        SystemHelper.Logger?.Trace($"Retrying aborted...", e);
                        return default;
                    } catch (AbortRetryAndThrowException e) {
                        SystemHelper.Logger?.Error($"Retrying abroted and throwing exception at the {attempted} time...", e);
                        throw;
                    } catch (Exception ex) {
                        Thread.Sleep(retryInterval);
                        SystemHelper.Logger?.Error($"Retrying for the {++attempted} time...", ex);
                        onError?.Invoke(attempted, ex);
                    }
                }
            else {
                for (; attempted < maxAttemptCount; attempted++)
                    try {
                        if (attempted > 0) Thread.Sleep(retryInterval);

                        return await action();
                    } catch (AbortRetryException e) {
                        SystemHelper.Logger?.Trace($"Retrying aborted...", e);
                        return default;
                    } catch (AbortRetryAndThrowException e) {
                        SystemHelper.Logger?.Error($"Retrying abroted and throwing exception at the {attempted} time...", e);
                        throw;
                    } catch (Exception ex) {
                        Thread.Sleep(retryInterval);
                        SystemHelper.Logger?.Error($"Retrying for the {++attempted} time...", ex);
                        onError?.Invoke(attempted, ex);
                        exception = ex;
                    }

                //throw an exception with current stack by recapturing.
                try {
                    throw new RetryException(exception);
                } catch (RetryException ex) {
                    //catch inorder to record stacktrace
                    SystemHelper.Logger?.Error($"Retried unsuccessfully for the {attempted} times...", ex);
                    if (@throw) {
                        throw;
                    }

                    return default;
                }
            }
        }
    }
}