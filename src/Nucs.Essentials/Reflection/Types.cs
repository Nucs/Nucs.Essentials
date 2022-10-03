using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Nucs.Exceptions;

namespace Nucs.DependencyInjection {
    /// <summary>
    ///     Stores <see cref="Known"/> for mapping between string name and the Type.
    /// </summary>
    public static class Types {
        public static readonly Dictionary<string, Type> Known;

        static Types() {
            try {
                Known = new Dictionary<string, Type>(128);
                Setup();
            } catch (Exception e) {
                SystemHelper.Logger?.Error(e);
                throw;
            }
        }

        public static void Setup() {
            IntroduceAttribute<BindAttribute>(type => !(type is null || type.IsAbstract));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TrySetup() {
            //trigger static constructor
        }

        public static void IntroduceAttribute<TAttribute>(Func<Type, bool> filter) where TAttribute : Attribute {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly.FullName?.StartsWith("System") != false)
                    continue;

                Type[] types;
                try {
                    types = assembly.GetTypes();
                } catch (ReflectionTypeLoadException e) {
                    continue;
                }

                foreach (var type in types) {
                    if (filter(type) && type.GetCustomAttributes(typeof(TAttribute), true)?.Length > 0) {
                        Known[type.Name] = type;
                        //s_logger.Debug(type.Name);
                    }
                }
            }
        }

        public static void IntroduceTypes(Func<Type, bool> filter) {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly.FullName?.StartsWith("System") != false)
                    continue;

                Type[] types;
                try {
                    types = assembly.GetTypes();
                } catch (ReflectionTypeLoadException) {
                    continue;
                }

                foreach (var type in types) {
                    Known[type.Name] = type;
                }
            }
        }

        public static void AssertMapped(string name) {
            if (Known.ContainsKey(name) == false)
                throw new DependecyInjectionException($"Unable to find type " + name);
        }
    }
}