using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nucs.Collections;
using Nucs.Collections.Structs;
using Nucs.Configuration;
using Nucs.Extensions;

namespace Nucs.Reflection {
    public delegate void ToListedDictionaryDelegate<in TDictionary>(TDictionary source, StreamWriter sw)
        where TDictionary : ListedDictionary<string, object?>;

    public delegate void ToListedDictionaryPullerDelegate<in TDictionary>(TDictionary source, object reporter)
        where TDictionary : ListedDictionary<string, object?>;

    public delegate void PullToStringDelegate<in TDictionary>(StructList<(string, object?)> source, StructList<(string, object?)>? destinition = null);

    public class ListedPropertyTag : ICloneable {
        public readonly string Name;
        public readonly int Index;
        public readonly Type Type;
        public object? Puller;
        public object? Reporter;

        /// <summary>
        ///     Is this property pulling from the child that starts the report?
        /// </summary>
        public bool ChildReporter;

        public Type? ChildReporterType;

        /// <summary>
        ///     Action&lt;T, StreamWriter&gt; but this object doesn't know about T.
        /// </summary>
        public Delegate? Stringifyer;

        /// <summary>
        ///     Action&lt;T, StreamWriter&gt; but this object doesn't know about T.
        /// </summary>
        public Action<object, StreamWriter> Writer;

        /// <summary>
        ///     Action&lt;T, StreamWriter&gt; but this object doesn't know about T.
        /// </summary>
        public LambdaExpression WriterExpression;

        public ListedPropertyTag(string name, int index, Type type) {
            Name = name;
            Index = index;
            Type = type;
            Puller = null;
        }

        public ListedPropertyTag(string name, int index, Type type, object? puller) {
            Name = name;
            Index = index;
            Type = type;
            Puller = puller;
        }

        public ListedPropertyTag Clone() {
            return new ListedPropertyTag(Name, Index, Type, Puller) { Reporter = Reporter, Stringifyer = Stringifyer, Writer = Writer, WriterExpression = WriterExpression };
        }

        object ICloneable.Clone() {
            return Clone();
        }
    }

    public static class ListedDictToCsvGenerator {
        private static PropertyInfo? s_dictionaryIndexer = typeof(Dictionary<string, object>).GetProperty("Item", typeof(object), new Type[] { typeof(string) });

        public static (Type PulledValueType, Expression? Puller) ProcessTag(ListedPropertyTag tag, ParameterExpression childParameter) {
            var propertyType = tag.Type;
            if (tag.Puller is null) {
                return (propertyType, null);
            }

            var pullerType = tag.Puller.GetType();
            var pullerGenericArguments = pullerType.GetGenericArguments();
            //try puller
            if (typeof(Expression).IsAssignableFrom(pullerType)) {
                //its a constant
                if (typeof(ConstantExpression).IsAssignableFrom(pullerType))
                    return (propertyType, (ConstantExpression) tag.Puller);
                if (tag.Puller is LambdaExpression lExpr) {
                    _reevaluateLambda:
                    var body = lExpr.Body;
                    var parameters = lExpr.Parameters;
                    if (parameters.Count == 0) {
                        //const/regular func
                        return (lExpr.ReturnType, body);
                    } else if (parameters.Count == 1) {
                        //has reporter
                        // ReSharper disable once UseMethodIsInstanceOfType
                        if (tag.ChildReporter) {
                            var inlinedLeftExpr = ExpressionHelper.ReplaceExpressionInsideLambda(lExpr, parameters[0], Expression.Convert(childParameter, tag.ChildReporterType!));
                            if (inlinedLeftExpr.Parameters.Count == 0) { //was it successful?
                                lExpr = inlinedLeftExpr;
                                goto _reevaluateLambda;
                            }

                            //fallback to calling the original delegate with constant.
                            return (lExpr.ReturnType, Expression.Invoke(lExpr, Expression.Convert(childParameter, tag.ChildReporterType!)));
                        } else {
                            var reporter = tag.Reporter;

                            if (reporter is null || !parameters[0].Type.IsAssignableFrom(reporter.GetType()))
                                throw new InvalidOperationException($"Unable to bind {pullerType.Name}<{(pullerGenericArguments[0].Name)}> when tag.Reporter not provided");

                            var inlinedLeftExpr = ExpressionHelper.ReplaceExpressionInsideLambda(lExpr, parameters[0], Expression.Constant(reporter, reporter.GetType()));
                            if (inlinedLeftExpr.Parameters.Count == 0) { //was it successful?
                                lExpr = inlinedLeftExpr;
                                goto _reevaluateLambda;
                            }

                            //fallback to calling the original delegate with constant.
                            return (lExpr.ReturnType, Expression.Invoke(lExpr, Expression.Constant(reporter, reporter.GetType())));
                        }
                    } else
                        throw new NotSupportedException();
                }

                var expression = (Expression) tag.Puller;
                pullerType = expression.Type;
                //is delegate, set source to calling it   
                var rawGenericType = pullerType.GetGenericTypeDefinition() ?? pullerType;
                var delCallerExpr = expression;
                throw new InvalidOperationException($"Unable to bind expression '{expression}' to {pullerType.Name}<{string.Join(",", pullerGenericArguments.Select(kv => kv.Name))}> when tag.Reporter not provided");
            }

            //try delegate
            if (typeof(Delegate).IsAssignableFrom(pullerType)) {
                var del = (Delegate) tag.Puller;
                //is delegate, set source to calling it   
                var rawGenericType = pullerType.GetGenericTypeDefinition() ?? pullerType;
                var delCallerExpr = del.Target is null ? null : Expression.Constant(del.Target, del.Target.GetType());
                MethodCallExpression delInvokerExpr;
                if (pullerType.IsGenericType) {
                    //its a func/action
                    if (rawGenericType == typeof(Func<>)) {
                        delInvokerExpr = Expression.Call(delCallerExpr, del.Method);
                    } else if (rawGenericType == typeof(Func< /*TReporter*/, /*TPulledValue*/>)) {
                        if (tag.ChildReporter) {
                            delInvokerExpr = Expression.Call(delCallerExpr, del.Method, Expression.Convert(childParameter, tag.ChildReporterType!));
                        } else {
                            if (tag.Reporter is null)
                                throw new InvalidOperationException($"Unable to bind Action<{(pullerGenericArguments[0].Name)}> when tag.Reporter not provided");
                            delInvokerExpr = Expression.Call(delCallerExpr, del.Method, Expression.Constant(tag.Reporter, tag.Reporter.GetType()));
                        }
                    } else if (rawGenericType == typeof(Action)) {
                        delInvokerExpr = Expression.Call(delCallerExpr, del.Method);
                    } else if (rawGenericType == typeof(Action< /*TReporter*/>)) {
                        if (tag.ChildReporter) {
                            delInvokerExpr = Expression.Call(delCallerExpr, del.Method, Expression.Convert(childParameter, tag.ChildReporterType!));
                        } else {
                            if (tag.Reporter is null)
                                throw new InvalidOperationException($"Unable to bind Action<{(pullerGenericArguments[0].Name)}> when tag.Reporter not provided");
                            delInvokerExpr = Expression.Call(delCallerExpr, del.Method, Expression.Constant(tag.Reporter, tag.Reporter.GetType()));
                        }
                    } else {
                        throw new NotSupportedException($"Unsupported delegate: {propertyType.FullName}");
                    }
                } else {
                    if (rawGenericType == typeof(Action)) {
                        delInvokerExpr = Expression.Call(delCallerExpr, del.Method);
                    } else
                        throw new NotSupportedException($"Unsupported delegate: {propertyType.FullName}");
                }

                return (del.Method.ReturnType, delInvokerExpr);
            }

            if (tag.Puller is not null)
                return (tag.Puller.GetType(), Expression.Constant(tag.Puller, tag.Puller.GetType()));

            return (propertyType, null);
        }


        private static void s_SetValue((string, object)[] array, int index, object value) {
            array[index].Item2 = value;
        }

        public static ToListedDictionaryPullerDelegate<TDictionary> CreatePuller<TDictionary>(
            #if DEBUG
            TDictionary map,
            #endif
            System.Collections.Generic.IList<ListedPropertyTag> properties, bool propertiesOptional = false, bool strongBoxStructures = false)
            where TDictionary : ListedDictionary<string, object?> {
            return CreatePullerExpression<TDictionary>(
                    #if DEBUG
                    map,
                    #endif
                    properties, propertiesOptional, strongBoxStructures
                )
               .Compile();
        }

        public static Expression<ToListedDictionaryPullerDelegate<TDictionary>> CreatePullerExpression<TDictionary>(
            #if DEBUG
            TDictionary map,
            #endif
            System.Collections.Generic.IList<ListedPropertyTag> properties, bool propertiesOptional = false, bool strongBoxStructures = false)
            where TDictionary : ListedDictionary<string, object?> {
            //parameters ---
            ParameterExpression dictionaryParameter = Expression.Parameter(typeof(TDictionary), "source");
            ParameterExpression childParameter = Expression.Parameter(typeof(object), "child");
            ParameterExpression[] parameters = new[] { dictionaryParameter, childParameter };
            List<ParameterExpression> variables = new();

            //body ---
            List<Expression> body = new List<Expression>();

            /*body.Add(Expression.IfThen(Expression.IsTrue(Expression.Equal(dictionaryParameter, Expression.Constant(null, typeof(object)))),
                                       Expression.Assign(dictionaryParameter, Expression.New(typeof(TDictionary)))));*/
            //shared vars
            var sourceArrayExpression = Expression.Variable(typeof((string, object?)[]), "sourceArray");
            variables.Add(sourceArrayExpression);

            body.Add(Expression.Assign(sourceArrayExpression, Expression.PropertyOrField(Expression.PropertyOrField(dictionaryParameter, nameof(ListedDictionary<string, object?>.InternalArray)), nameof(StructList<(string, object?)>.InternalArray))));

            //  handle each property
            foreach (ListedPropertyTag prop in properties) {
                if (string.IsNullOrEmpty(prop.Name)) {
                    SystemHelper.Logger?.Error($"Passed a property to {nameof(CreatePuller)} that is null or empty, see stacktrace" + "\n" + Environment.StackTrace);
                    continue;
                }

                #if DEBUG
                //assert index location
                if (prop.Index != map.IndexOf(prop.Name))
                    throw new DynamicGenerationException($"{prop.Name} was not found inside the given parameter 'map' of ListedDictionary at the same index (Prop came in with {prop.Index} where map is at {map.IndexOf(prop.Name)}.");
                #endif

                //factorizing this pattern for each property
                //sw.Write(',');
                //sw.Write(map[1].Value);

                //get of type 'object'
                (Type pullerReturnType, Expression? puller) = ProcessTag(prop, childParameter);
                if (puller is null) continue;

                var targetIndexValue = Expression.PropertyOrField(Expression.ArrayIndex(sourceArrayExpression, Yield(Expression.Constant(prop.Index, typeof(int)))), "Item2");

                //take care of conversion to object
                if (strongBoxStructures && pullerReturnType.IsValueType) {
                    var staticBoxGetter = ToDictionaryGenerator.s_poolGetter.GetOrAdd(pullerReturnType, ToDictionaryGenerator.StaticPoolGetter);
                    if (prop.ChildReporter) {
                        body.Add(Expression.Assign(targetIndexValue, Expression.Convert(Expression.Call(null, staticBoxGetter, new[] { puller }), typeof(object)))); //always get a new box for child.
                    } else {
                        body.Add(Expression.Assign(targetIndexValue, Expression.Convert(Expression.Call(null, staticBoxGetter, new[] { puller }), typeof(object)))); //always get a new box for child.
                        //body.Add(Expression.IfThenElse(Expression.Equal(targetIndexValue, Expression.Constant(null, typeof(object))),
                        //                               Expression.Assign(targetIndexValue, Expression.Convert(Expression.Call(null, staticBoxGetter, new[] { puller }), typeof(object))),
                        //                               Expression.Assign(Expression.Field(Expression.Convert(targetIndexValue, typeof(PooledStrongBox<>).MakeGenericType(pullerReturnType)), "Value"), puller)));
                    }

                    continue;
                }

                if (puller.Type != typeof(object))
                    puller = Expression.Convert(puller, typeof(object));

                body.Add(Expression.Assign(targetIndexValue, puller));
            }

            //create lambda and return ---
            BlockExpression block = Expression.Block(variables, body);
            Expression<ToListedDictionaryPullerDelegate<TDictionary>> lambda = Expression.Lambda<ToListedDictionaryPullerDelegate<TDictionary>>(block, parameters);

            if (lambda.CanReduce) {
                lambda = (Expression<ToListedDictionaryPullerDelegate<TDictionary>>) lambda.Reduce();
            }

            return lambda;
        }

        public static PullToStringDelegate<TDictionary> FlushPullToStream<TDictionary>(
            #if DEBUG
            TDictionary map,
            #endif
            System.Collections.Generic.IList<ListedPropertyTag> properties, bool propertiesOptional = false, bool strongBoxStructures = false)
            where TDictionary : ListedDictionary<string, object?> {
            //parameters ---
            ParameterExpression sourceParameter = Expression.Parameter(typeof(StructList<(string, object?)>), "source");
            ParameterExpression destInputParameter = Expression.Parameter(typeof(StructList<(string, object?)>?), "dest");
            ParameterExpression[] parameters = new[] { sourceParameter, destInputParameter };
            List<ParameterExpression> variables = new();
            ParameterExpression destParameter = Expression.Parameter(typeof(StructList<(string, object?)>?), "destResolved");
            variables.Add(destParameter);

            //body ---
            List<Expression> body = new List<Expression>();
            body.Add(Expression.Assign(destParameter, Expression.Coalesce(destInputParameter, sourceParameter))); //destParameter = destParameter ?? sourceParameter;

            //shared vars
            var destArrayExpression = Expression.Variable(typeof((string, object?)[]), "destArray");
            variables.Add(destArrayExpression);

            body.Add(Expression.Assign(destArrayExpression, Expression.PropertyOrField(destParameter, nameof(StructList<(string, object?)>.InternalArray))));
            var assignValueMethod = typeof(ListedDictToCsvGenerator).GetMethod(nameof(s_SetValue), BindingFlags.NonPublic | BindingFlags.Static);
            if (assignValueMethod is null)
                throw new ArgumentException(nameof(assignValueMethod));

            //  handle each property
            foreach (ListedPropertyTag prop in properties) {
                if (string.IsNullOrEmpty(prop.Name)) {
                    SystemHelper.Logger?.Error($"Passed a property to {nameof(CreatePuller)} that is null or empty, see stacktrace" + "\n" + Environment.StackTrace);
                    continue;
                }

                #if DEBUG
                //assert index location
                if (prop.Index != map.IndexOf(prop.Name))
                    throw new DynamicGenerationException($"{prop.Name} was not found inside the given parameter 'map' of ListedDictionary at the same index (Prop came in with {prop.Index} where map is at {map.IndexOf(prop.Name)}.");
                #endif

                //factorizing this pattern for each property
                //sw.Write(',');
                //sw.Write(map[1].Value);

                //get of type 'object'
                (Type pullerReturnType, Expression? puller) = ProcessTag(prop, null); /*TODO 2nd argument*/
                if (puller is null) continue;

                if (ConfigParsers.ExpressionToStringConverters.TryGetValue(pullerReturnType, out LambdaExpression? parser)) {
                    if (pullerReturnType.IsValueType && strongBoxStructures) {
                        var valueExpr = Expression.PropertyOrField(Expression.ArrayIndex(destArrayExpression, Yield(Expression.Constant(prop.Index, typeof(int)))), "Item2");
                        var strongboxUnpack = typeof(PooledStrongBox<>).MakeGenericType(pullerReturnType).GetMethod(nameof(IUnsafeStrongbox.UnboxAs)).MakeGenericMethod(pullerReturnType);

                        body.Add(Expression.Assign(valueExpr,
                                                   Expression.Invoke(parser, Expression.Call(strongboxUnpack, valueExpr))));
                    } else {
                        body.Add(Expression.Assign(Expression.PropertyOrField(Expression.ArrayIndex(destArrayExpression, Yield(Expression.Constant(prop.Index, typeof(int)))), "Item2"),
                                                   Expression.Invoke(parser, Expression.PropertyOrField(Expression.ArrayIndex(destArrayExpression, Yield(Expression.Constant(prop.Index, typeof(int)))), "Item2"))));
                    }
                    //we have a converter
                }
            }

            //create lambda and return ---
            BlockExpression block = Expression.Block(variables, body);
            Expression<PullToStringDelegate<TDictionary>> lambda = Expression.Lambda<PullToStringDelegate<TDictionary>>(block, parameters);

            if (lambda.CanReduce) {
                lambda = (Expression<PullToStringDelegate<TDictionary>>) lambda.Reduce();
            }

            return lambda.Compile();
        }

        private static IEnumerable<T> Yield<T>(T obj) {
            yield return obj;
        }

        private static IEnumerable<T> Yield<T>(T obj, T obj2) {
            yield return obj;
            yield return obj2;
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