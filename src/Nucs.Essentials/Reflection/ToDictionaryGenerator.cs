using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Nucs.Collections;

namespace Nucs.Reflection {
    [Serializable]
    public class DynamicGenerationException : Exception {
        public DynamicGenerationException() { }
        public DynamicGenerationException(string message) : base(message) { }
        public DynamicGenerationException(string message, Exception inner) : base(message, inner) { }

        protected DynamicGenerationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }

    public delegate TDictionary ToDictionaryDelegate<in T, TDictionary>(T input, TDictionary existing = null) where TDictionary : class, IDictionary<string, object>;

    public static class ToDictionaryGenerator {
        public static ToDictionaryDelegate<T, Dictionary<string, object>> CreateGetter<T>(IList<string> properties, bool propertiesOptional = false, bool strongBoxStructures = false) {
            return CreateGetter<T, Dictionary<string, object>>(properties, propertiesOptional, strongBoxStructures);
        }

        public static ToDictionaryDelegate<T, ListedDictionary<string, object>> CreateOrderedGetter<T>(IList<string> properties, bool propertiesOptional = false, bool strongBoxStructures = false) {
            return CreateGetter<T, ListedDictionary<string, object>>(properties, propertiesOptional, strongBoxStructures);
        }

        public static ToDictionaryDelegate<T, ConcurrentDictionary<string, object>> CreateConcurrentGetter<T>(IList<string> properties, bool propertiesOptional = false, bool strongBoxStructures = false) {
            return CreateGetter<T, ConcurrentDictionary<string, object>>(properties, propertiesOptional, strongBoxStructures);
        }

        public static ToDictionaryDelegate<T, TDictionary> CreateGetter<T, TDictionary>(IList<string> properties, bool propertiesOptional = false, bool strongBoxStructures = false) where TDictionary : class, IDictionary<string, object> {
            //parameters ---
            ParameterExpression inputParameter = Expression.Parameter(typeof(T), "input");
            ParameterExpression dictionaryParameter = Expression.Parameter(typeof(TDictionary), "existing");
            ParameterExpression[] parameters = new ParameterExpression[] { inputParameter, dictionaryParameter };

            //body ---
            List<Expression> body = new List<Expression>();

            //  if (dictionaryParameter == null)
            //      dictionaryParameter = new TDictionary<string, object>();
            body.Add(Expression.IfThen(Expression.IsTrue(Expression.Equal(dictionaryParameter, Expression.Constant(null, typeof(object)))),
                                       Expression.Assign(dictionaryParameter, Expression.New(typeof(TDictionary)))));

            //  handle each property
            PropertyInfo[] props = AutoConfig.StrategyProperties(typeof(T));
            PropertyInfo? dictionaryIndexer = typeof(TDictionary).GetProperty("Item");
            if (dictionaryIndexer == null || dictionaryIndexer.GetIndexParameters().Length != 1)
                throw new ArgumentException(nameof(dictionaryIndexer));

            foreach (string propertyName in properties) {
                if (string.IsNullOrEmpty(propertyName)) {
                    SystemHelper.Logger?.Error("Passed a property to ToDictionary that is null or empty, see stacktrace" + "\n" + Environment.StackTrace);
                    continue;
                }

                PropertyInfo prop = null;
                for (int i = 0; i < props.Length; i++) {
                    if (props[i] != null && propertyName.Equals(props[i].Name)) {
                        prop = props[i];
                        break;
                    }
                }

                if (prop == null) {
                    if (propertiesOptional)
                        continue;
                    throw new DynamicGenerationException($"{propertyName} was not found inside the type {typeof(T)}.");
                }

                IndexExpression assignTarget = Expression.MakeIndex(dictionaryParameter, dictionaryIndexer, Yield(Expression.Constant(prop.Name, typeof(string))));
                Expression source = Expression.Property(inputParameter, prop);

                Type propertyType = prop.PropertyType;
                Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                if (prop.PropertyType.IsValueType) {
                    if (strongBoxStructures) {
                        var staticBoxGetter = s_poolGetter.GetOrAdd(underlyingType, StaticPoolGetter);

                        if (underlyingType != propertyType) {
                            source = Expression.Condition(
                                Expression.Property(source, "HasValue"),
                                Expression.Convert(Expression.Call(null, staticBoxGetter, new[] { Expression.Property(source, "Value") }), typeof(object)),
                                Expression.Constant(null, typeof(object)));
                        } else
                            source = Expression.Convert(Expression.Call(null, staticBoxGetter, new[] { source }), typeof(object));
                    } else {
                        source = Expression.Convert(source, typeof(object));
                    }
                }

                body.Add(Expression.Assign(assignTarget, source));
            }

            //return ---
            body.Add(dictionaryParameter); //return dictionary

            //create lambda and return ---
            BlockExpression block = Expression.Block(body);
            Expression<ToDictionaryDelegate<T, TDictionary>> lambda = Expression.Lambda<ToDictionaryDelegate<T, TDictionary>>(block, parameters);

            return lambda.Compile();
        }

        internal static readonly ConcurrentDictionary<Type, MethodInfo> s_poolGetter = new();

        internal static MethodInfo StaticPoolGetter(Type type) {
            return typeof(PooledStrongBox<>).MakeGenericType(type).GetMethods()
                                            .FirstOrDefault(g => g.Name == "Get"
                                                                 && g.GetParameters() is { Length: >0 } && !g.GetParameters()[0].ParameterType.IsByRef);
        }

        private static IEnumerable<T> Yield<T>(T obj) {
            yield return obj;
        }

        public static void SetPropertyValue<T, TValue>(this T target, Expression<Func<T, TValue>> memberLamda, TValue value) where T : class {
            var memberSelectorExpression = memberLamda.Body as MemberExpression;
            if (memberSelectorExpression != null) {
                var property = memberSelectorExpression.Member as PropertyInfo;
                if (property != null) {
                    property.SetValue(target, value, null);
                }
            }
        }

        public static void SetPropertyValue<T, TValue>(this T target, Expression<Func<T, TValue>> memberLamda, ref TValue value) where T : struct {
            var memberSelectorExpression = memberLamda.Body as MemberExpression;
            if (memberSelectorExpression != null) {
                var property = memberSelectorExpression.Member as PropertyInfo;
                if (property != null) {
                    property.SetValue(target, value, null);
                }
            }
        }
    }
}