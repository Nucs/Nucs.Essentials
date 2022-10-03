using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Nucs.Reflection {
    public class PropertyGetter<T> {
        private static readonly ConcurrentDictionary<string, Delegate> Getters = new ConcurrentDictionary<string, Delegate>();

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static Func<T, TOut> GetFunction<TOut>(string memberName) {
            return (Func<T, TOut>) Getters.GetOrAdd(memberName, memberName => (Func<T, TOut>) (Getters[memberName] = InternalGetFunction<TOut>(memberName)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Get<TOut>(T target, string memberName) {
            return GetFunction<TOut>(memberName)(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static Func<T, TOut> InternalGetFunction<TOut>(string memberName) {
            var parameter = Expression.Parameter(typeof(T), "obj");
            var lambda = Expression.Lambda<Func<T, TOut>>(Expression.PropertyOrField(parameter, memberName), new ParameterExpression[] { parameter });
            return lambda.Compile();
        }
    }
}