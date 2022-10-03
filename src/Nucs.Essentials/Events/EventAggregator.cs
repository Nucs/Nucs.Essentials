using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Reflection;

namespace Nucs.Events {
    public class EventAggregator {
        private readonly ConcurrentDictionary<Type, IDelegate> _handlers = new();
        private readonly ConcurrentDictionary<(string Symbol, Type MessageType), IDelegate> _symbolHandlers = new();

        internal EventAggregator() { }

        public virtual bool HandlerExistsFor(Type messageType) {
            return _handlers.ContainsKey(messageType);
        }

        public virtual bool HandlerExistsFor(string symbol, Type messageType) {
            return _symbolHandlers.ContainsKey((symbol, messageType));
        }

        public bool HandlerExistsFor<TArgument>() {
            return _handlers.ContainsKey(typeof(TArgument));
        }

        public bool HandlerExistsFor<TArgument>(string symbol) {
            return _symbolHandlers.ContainsKey((symbol, typeof(TArgument)));
        }

        public int Subscribe<TArguments>(EventDelegate<TArguments> callback) {
            var eve = _handlers.GetOrAdd(typeof(TArguments), type => new Delegate<TArguments>());
            return eve.Subscribe(callback);
        }

        public int Subscribe<TArguments>(string symbol, EventDelegate<TArguments> callback) {
            var eve = _symbolHandlers.GetOrAdd((symbol, typeof(TArguments)), type => new Delegate<TArguments>());
            return eve.Subscribe(callback);
        }

        public int Subscribe<TArguments>(RefEventDelegate<TArguments> callback) {
            var eve = _handlers.GetOrAdd(typeof(TArguments), type => new RefDelegate<TArguments>());
            return eve.Subscribe(callback);
        }

        public int Subscribe<TArguments>(string symbol, RefEventDelegate<TArguments> callback) {
            var eve = _symbolHandlers.GetOrAdd((symbol, typeof(TArguments)), type => new RefDelegate<TArguments>());
            return eve.Subscribe(callback);
        }

        public bool Unsubscribe<TArguments>(int token) {
            if (token <= 0) return false;
            if (_handlers.TryGetValue(typeof(TArguments), out var eve)) {
                return eve.Unsubscribe(token);
            }

            return false;
        }

        public bool Unsubscribe<TArguments>(string symbol, int token) {
            if (token <= 0) return false;
            if (_symbolHandlers.TryGetValue((symbol, typeof(TArguments)), out var eve)) {
                return eve.Unsubscribe(token);
            }

            return false;
        }

        public void Publish<TArgument>(object sender, TArgument message) {
            if (_handlers.TryGetValue(typeof(TArgument), out var eve)) {
                eve.Invoke(sender, message);
            }
        }

        public void Publish<TArgument>(TArgument message) {
            if (_handlers.TryGetValue(typeof(TArgument), out var eve)) {
                eve.Invoke(null, message);
            }
        }

        public Task<TArgument> PublishAsync<TArgument>(TArgument message) where TArgument : class {
            if (_handlers.TryGetValue(typeof(TArgument), out var eve)) {
                var task = new TaskCompletionSource<TArgument>();
                ThreadPool.QueueUserWorkItem(state => {
                    try {
                        eve.Invoke(null, message);
                        task.TrySetResult(message);
                    } catch (Exception e) {
                        task.TrySetException(e);
                    }
                });

                return task.Task;
            } else
                return Task.FromResult(message);
        }

        public Task<StrongBox<TArgument>> PublishStructAsync<TArgument>(ref TArgument message) where TArgument : struct {
            if (!_handlers.TryGetValue(typeof(TArgument), out var eve))
                return Task.FromResult(new StrongBox<TArgument>(ref message));
            var task = new TaskCompletionSource<StrongBox<TArgument>>();
            var box = new StrongBox<TArgument>(ref message);
            ThreadPool.QueueUserWorkItem(state => {
                try {
                    eve.Invoke(null, ref box.Value);
                    task.TrySetResult(box);
                } catch (Exception e) {
                    task.TrySetException(e);
                }
            });
            return task.Task;
        }

        public Task<TArgument> PublishAsync<TArgument>(string symbol, TArgument message) where TArgument : class {
            if (!_symbolHandlers.TryGetValue((symbol, typeof(TArgument)), out var eve))
                return Task.FromResult(message);

            var task = new TaskCompletionSource<TArgument>();
            ThreadPool.QueueUserWorkItem(state => {
                try {
                    eve.Invoke(symbol, message);
                    task.TrySetResult(message);
                } catch (Exception e) {
                    task.TrySetException(e);
                }
            });

            return task.Task;
        }

        public void PublishAsyncForget<TArgument>(TArgument message) where TArgument : class {
            if (_handlers.TryGetValue(typeof(TArgument), out var eve)) {
                ThreadPool.QueueUserWorkItem(state => eve.Invoke(null, message));
            }
        }

        public void PublishStructAsyncForget<TArgument>(ref TArgument message) where TArgument : struct {
            if (!_handlers.TryGetValue(typeof(TArgument), out var eve))
                return;

            var box = new StrongBox<TArgument>(ref message);
            ThreadPool.QueueUserWorkItem(state => { eve.Invoke(null, ref box.Value); });
        }

        public void PublishAsyncForget<TArgument>(string symbol, TArgument message) where TArgument : class {
            if (_symbolHandlers.TryGetValue((symbol, typeof(TArgument)), out var eve)) {
                ThreadPool.QueueUserWorkItem(state => eve.Invoke(symbol, message));
            }
        }

        /*
        public Task<TArgument> PublishGuiAsync<TArgument>(TArgument message, DispatcherPriority priority = DispatcherPriority.Normal) where TArgument : class {
            if (!_handlers.TryGetValue(typeof(TArgument), out var eve))
                return Task.FromResult(message);

            var task = new TaskCompletionSource<TArgument>();
            Application.Current.Dispatcher.BeginInvoke(() => {
                try {
                    eve.Invoke(null, message);
                    task.TrySetResult(message);
                } catch (Exception e) {
                    task.TrySetException(e);
                }
            }, priority);

            return task.Task;
        }

        public Task<TArgument> PublishGuiAsync<TArgument>(string symbol, TArgument message, DispatcherPriority priority = DispatcherPriority.Normal) where TArgument : class {
            if (!_symbolHandlers.TryGetValue((symbol, typeof(TArgument)), out var eve))
                return Task.FromResult(message);
            var task = new TaskCompletionSource<TArgument>();
            Application.Current.Dispatcher.BeginInvoke(() => {
                try {
                    eve.Invoke(symbol, message);
                    task.TrySetResult(message);
                } catch (Exception e) {
                    task.TrySetException(e);
                }
            }, priority);

            return task.Task;
        }

        public void PublishGuiAsyncForget<TArgument>(TArgument message, DispatcherPriority priority = DispatcherPriority.Normal) where TArgument : class {
            if (_handlers.TryGetValue(typeof(TArgument), out var eve))
                Application.Current?.Dispatcher.BeginInvoke(() => eve.Invoke(null, message), priority);
        }

        public void PublishGuiAsyncForget<TArgument>(string symbol, TArgument message, DispatcherPriority priority = DispatcherPriority.Normal) where TArgument : class {
            if (_symbolHandlers.TryGetValue((symbol, typeof(TArgument)), out var eve))
                Application.Current.Dispatcher.BeginInvoke(() => eve.Invoke(symbol, message), priority);
        }*/

        public void Publish<TArgument>(ref TArgument message) {
            if (!_handlers.TryGetValue(typeof(TArgument), out var eve))
                return;
            eve.Invoke(null, ref message);
        }

        public void PublishStruct<TArgument>(TArgument message) where TArgument : struct {
            if (!_handlers.TryGetValue(typeof(TArgument), out var eve))
                return;
            eve.Invoke(null, ref message);
        }

        public void PublishStruct<TArgument>(ref TArgument message) where TArgument : struct {
            if (!_handlers.TryGetValue(typeof(TArgument), out var eve))
                return;
            eve.Invoke(null, ref message);
        }

        public void Publish<TArgument>(string symbol, TArgument message) {
            if (_symbolHandlers.TryGetValue((symbol, typeof(TArgument)), out var eve)) {
                eve.Invoke(symbol, message);
            }
        }

        public void Publish<TArgument>(string symbol, ref TArgument message) {
            if (_symbolHandlers.TryGetValue((symbol, typeof(TArgument)), out var eve)) {
                eve.Invoke(symbol, ref message);
            }
        }

        public void Publish<TArgument>(object sender, string symbol, TArgument message) {
            if (_symbolHandlers.TryGetValue((symbol, typeof(TArgument)), out var eve)) {
                eve.Invoke(sender ?? symbol, message);
            }
        }
    }
}