using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nucs.Reflection {
    public class StructToString<T> {
        //private static readonly MethodInfo s_toStringMethodInfo;
        private static readonly Func<T, string> s_getters;
        public static readonly Expression<Func<T, string>> GetterExpression;

        static StructToString() {
            var toStringMethodInfo = typeof(T).GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, Array.Empty<Type>(), null);
            var parameter = Expression.Parameter(typeof(T), "obj");
            var lambda = Expression.Lambda<Func<T, string>>(Expression.Call(parameter, toStringMethodInfo), new ParameterExpression[] { parameter });
            GetterExpression = lambda;
            s_getters = lambda.Compile();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(T target) {
            return s_getters(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(ref T target) {
            return s_getters(target);
        }
    }
}