using System;
using System.Threading.Tasks;

namespace Nucs.Events {
    public static class Events {
        internal static readonly EventAggregator Instance = new EventAggregator();

        public static bool HandlerExistsFor(Type messageType) {
            return Instance.HandlerExistsFor(messageType);
        }

        public static bool HandlerExistsFor(string symbol, Type messageType) {
            return Instance.HandlerExistsFor(symbol, messageType);
        }

        public static bool HandlerExistsFor<TArgument>() {
            return Instance.HandlerExistsFor<TArgument>();
        }

        public static bool HandlerExistsFor<TArgument>(string symbol) {
            return Instance.HandlerExistsFor<TArgument>(symbol);
        }

        public static int Subscribe<TArguments>(EventDelegate<TArguments> callback) {
            return Instance.Subscribe(callback);
        }

        public static int Subscribe<TArguments>(string symbol, EventDelegate<TArguments> callback) {
            return Instance.Subscribe<TArguments>(symbol, callback);
        }

        public static int Subscribe<TArguments>(RefEventDelegate<TArguments> callback) {
            return Instance.Subscribe(callback);
        }

        public static int Subscribe<TArguments>(string symbol, RefEventDelegate<TArguments> callback) {
            return Instance.Subscribe(symbol, callback);
        }

        public static bool Unsubscribe<TArguments>(int token) {
            return Instance.Unsubscribe<TArguments>(token);
        }

        public static bool Unsubscribe<TArguments>(string symbol, int token) {
            return Instance.Unsubscribe<TArguments>(symbol, token);
        }

        public static TArgument Publish<TArgument>(object sender, TArgument message) {
            Instance.Publish(sender, message);
            return message;
        }

        public static Task<TArgument> PublishAsync<TArgument>(TArgument message) where TArgument : class {
            return Instance.PublishAsync(message);
        }

        public static void PublishAsyncForget<TArgument>(TArgument message) where TArgument : class {
            Instance.PublishAsyncForget(message);
        }

        public static Task<TArgument> PublishAsync<TArgument>(string symbol, TArgument message) where TArgument : class {
            return Instance.PublishAsync(symbol, message);
        }

        public static void PublishAsyncForget<TArgument>(string symbol, TArgument message) where TArgument : class {
            Instance.PublishAsyncForget(symbol, message);
        }

        /*public static Task<TArgument> PublishGuiAsync<TArgument>(TArgument message, DispatcherPriority priority = DispatcherPriority.Normal) where TArgument : class {
            return Instance.PublishGuiAsync(message, priority);
        }

        public static Task<TArgument> PublishGuiAsync<TArgument>(string symbol, TArgument message, DispatcherPriority priority = DispatcherPriority.Normal) where TArgument : class {
            return Instance.PublishGuiAsync(symbol, message, priority);
        }

        public static void PublishGuiAsyncForget<TArgument>(TArgument message, DispatcherPriority priority = DispatcherPriority.Normal) where TArgument : class {
            Instance.PublishGuiAsyncForget(message, priority);
        }

        public static void PublishGuiAsyncForget<TArgument>(string symbol, TArgument message, DispatcherPriority priority = DispatcherPriority.Normal) where TArgument : class {
            Instance.PublishGuiAsyncForget(symbol, message, priority);
        }*/

        public static TArgument Publish<TArgument>(TArgument message) {
            Instance.Publish(ref message);
            return message;
        }

        public static void PublishStruct<TArgument>(TArgument message) where TArgument : struct {
            Instance.Publish(ref message);
        }

        public static void PublishStruct<TArgument>(ref TArgument message) where TArgument : struct {
            Instance.Publish(ref message);
        }

        public static void PublishStruct<TArgument>(string symbol, TArgument message) where TArgument : struct {
            Instance.Publish(symbol, ref message);
        }

        public static void PublishStruct<TArgument>(string symbol, ref TArgument message) where TArgument : struct {
            Instance.Publish(symbol, ref message);
        }

        public static TArgument PublishStruct<TArgument>(string symbol) where TArgument : struct {
            TArgument message = default;
            Instance.Publish(symbol, ref message);
            return message;
        }

        public static TArgument PublishStruct<TArgument>() where TArgument : struct {
            TArgument message = default;
            Instance.Publish(ref message);
            return message;
        }

        public static TArgument Publish<TArgument>(string symbol, TArgument message) {
            Instance.Publish<TArgument>(symbol, message);
            return message;
        }

        public static TArgument Publish<TArgument>(object sender, string symbol, TArgument message) {
            Instance.Publish(sender, symbol, message);
            return message;
        }
    }
}