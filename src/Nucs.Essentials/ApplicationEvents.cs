using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucs.Collections;
using Nucs.Collections.Structs;
using Nucs.Disposable;
using Nucs.Extensions;

namespace Nucs {
    public delegate void ExceptionHandler(Exception exc, bool isTerminating);

    public delegate void ApplicationFinishedHandler(string state);

    /// <summary>
    ///    Provides an api to intercept app-wide events such process exiting
    /// </summary>
    public static class ApplicationEvents {
        /// <summary>
        ///     Logger responsible of reporting application events provided in this file.
        /// </summary>
        public static ILogger? Logger;

        public static readonly ConcurrentList<IDisposable> Disposables = new ConcurrentList<IDisposable>();
        public static readonly ConcurrentList<Task> Awaitables = new ConcurrentList<Task>();

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        static ApplicationEvents() {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            #if !DEBUG
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnFirstChanceException;
            #endif
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
        }

        public static event Action ProcessExiting;
        public static event ApplicationFinishedHandler ApplicationFinished;
        public static event ExceptionHandler ExceptionOccured;

        public static void OnProcessExiting() {
            Logger?.LogInformation("Process is exiting...");
            ProcessExiting?.Invoke();
        }

        public static void OnException(Exception exc, bool isTerminating) {
            //logger.ErrorException(exc.Message, exc);
            ExceptionOccured?.Invoke(exc, isTerminating);
        }

        public static void OnApplicationFinished(string state) {
            Logger?.LogInformation("Application execution finished with state: " + state);
            ApplicationFinished?.Invoke(state);
        }

        /// <summary>
        ///     Registers <paramref name="disposable"/> to dispose when application exits.
        /// </summary>
        public static void RegisterDispose(IDisposable disposable) {
            ProcessExiting += Handler;

            void Handler() {
                ProcessExiting -= Handler;
                try {
                    disposable?.Dispose();
                } catch (Exception e) {
                    Logger?.LogError($"Unable to dispose {disposable?.GetType().FullName} due to exception.", e);
                }
            }
        }

        public static void OnExitNormally() {
            Logger?.LogInformation($"ApplicationEvents.OnExitNormally is awaiting and finalizing writers");
            while (Awaitables._count != 0) {
                Awaitables.Lock.EnterWriteLock();
                StructList<Task> awaiting;
                try {
                    awaiting = Awaitables.AsUnlockedSpan.ToArray().AsStructList()!;
                    Array.Clear(Awaitables._arr, 0, Awaitables._count);
                    Awaitables._count = 0;
                } finally {
                    Awaitables.Lock.ExitWriteLock();
                }

                awaiting.RemoveAll(new PredicateByRef<Task>((ref Task item) => item is null || item.IsCompleted));

                if (awaiting.Count > 0) { //while loop incase more are added after this group was awaited
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (awaiting.Count > 0)
                        try {
                            Task.WaitAll(awaiting.ToArray()!);
                        } catch (Exception e) {
                            Logger?.LogError($"Error while awaiting ApplicationEvents. OnExitNormally: " + e.ToString());
                        }
                }
            }

            if (Disposables.Count > 0) {
                var disposables = Disposables.ToArray();
                Disposables.Clear();
                foreach (var d in disposables) {
                    d.SafeDispose();
                }
            }

            Logger?.LogInformation($"ApplicationEvents.OnExitNormally has finalized...");
        }

        #region Binds

        private static void CurrentDomainOnFirstChanceException(object sender, FirstChanceExceptionEventArgs e) {
            OnException(e.Exception, false);
        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs e) {
            OnProcessExiting();
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
            OnException((Exception)e.ExceptionObject, e.IsTerminating);
        }

        #endregion

        public static void EnsureAwaited(this Task? awaiter) {
            if (awaiter is null || awaiter.IsCompleted)
                return;
            Awaitables.Add(awaiter);
        }

        public static void EnsureDisposed(IDisposable? awaiter) {
            if (awaiter is null)
                return;
            Disposables.Add(awaiter);
        }

        public static void EnsureDisposed(Action? act) {
            if (act is null)
                return;
            Disposables.Add(new DisposableWrapper(act));
        }
    }
}