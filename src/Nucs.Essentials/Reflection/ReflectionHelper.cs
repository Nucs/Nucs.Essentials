using System;
using System.Linq;
using System.Reflection;

namespace Nucs.Reflection {
    public static class ReflectionHelper {
        /// <summary>
        ///     Does <paramref name="testType"/> implements <paramref name="interfaceType"/> and not inheriting it from parent.
        /// </summary>
        /// <returns>True if <paramref name="testType"/> directly implements <paramref name="interfaceType"/></returns>
        public static bool DirectlyImplementsInterface(this Type testType, Type interfaceType) {
            if (!interfaceType.IsInterface || !testType.IsClass || !interfaceType.IsAssignableFrom(testType))
                return false;

            var parent = testType.BaseType;
            if (parent is null || parent == typeof(object))
                return false;
            return !testType.IsAssignableFrom(parent); //parent must not implement this interface.
        }


        public static bool AreTypesEqual<T>(this T x, object obj) {
            return typeof(T) == obj?.GetType();
        }

        public static bool AreTypesEqual<T, T2>(this T x, T2 obj) {
            return typeof(T) == typeof(T2);
        }

        public static bool IsIndexer(this PropertyInfo member) =>
            (uint) member.GetIndexParameters().Length > 0U;

        public static PropertyInfo? GetSingleIndexer(this Type type) {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
                       .SingleOrDefault((Func<PropertyInfo?, bool>) (p => p.IsIndexer()));
        }
    }
}