using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Nucs.Configuration;
using Nucs.Extensions;

namespace Nucs.Reflection {
    public static class PreloadedPropertyGetter<T> {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly Dictionary<string, Delegate> Getters = new();
        public static readonly Dictionary<string, Expression> GettersExpressions = new();
        public static readonly Dictionary<string, Delegate> ToStringGetters = new();
        public static readonly Dictionary<string, Expression> ToStringGettersExpressions = new();
        public static readonly Dictionary<string, Delegate> StaticGetters = new();
        public static readonly Dictionary<string, Expression> StaticGettersExpressions = new();
        public static readonly Dictionary<string, Delegate> StaticToStringGetters = new();
        public static readonly Dictionary<string, Expression> StaticToStringGettersExpressions = new();

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static void Preload() {
            // ReSharper disable once CoVariantArrayConversion
            Preload(AutoConfig.StrategyProperties(typeof(T)));
            // ReSharper disable once CoVariantArrayConversion
            Preload(AutoConfig.StrategyFields(typeof(T)));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static void PreloadStatics() {
            // ReSharper disable once CoVariantArrayConversion
            PreloadStatic(typeof(T).GetProperties(BindingFlags.Static | BindingFlags.Public));
            PreloadStatic(typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static Func<T, TOut> GetFunction<TOut>(string memberName) {
            return (Func<T, TOut>) Getters[memberName];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static bool TryGetFunction<TOut>(string memberName, out Func<T, TOut>? func) {
            if (!Getters.TryGetValue(memberName, out var del)) {
                func = null;
                return false;
            }

            func = (Func<T, TOut>) del;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static Expression<Func<T, TOut>> GetExpression<TOut>(string memberName) {
            return (Expression<Func<T, TOut>>) GettersExpressions[memberName];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static bool TryGetExpression<TOut>(string memberName, out Expression<Func<T, TOut>>? func) {
            if (!GettersExpressions.TryGetValue(memberName, out var del)) {
                func = null;
                return false;
            }

            func = (Expression<Func<T, TOut>>) del;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static LambdaExpression GetExpression(string memberName) {
            return (LambdaExpression) GettersExpressions[memberName];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static bool TryGetExpression(string memberName, out LambdaExpression? func) {
            if (!GettersExpressions.TryGetValue(memberName, out var del)) {
                func = null;
                return false;
            }

            func = (LambdaExpression) del;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static bool TryGetStaticFunction<TOut>(string memberName, out Func<T, TOut>? func) {
            if (!StaticGetters.TryGetValue(memberName, out var del)) {
                func = null;
                return false;
            }

            func = (Func<T, TOut>) del;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static Expression<Func<TOut>> GetStaticExpression<TOut>(string memberName) {
            return (Expression<Func<TOut>>) StaticGettersExpressions[memberName];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static bool TryGetStaticExpression<TOut>(string memberName, out Expression<Func<TOut>>? func) {
            if (!StaticGettersExpressions.TryGetValue(memberName, out var del)) {
                func = null;
                return false;
            }

            func = (Expression<Func<TOut>>) del;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static Func<TOut> GetStaticFunction<TOut>(string memberName) {
            return (Func<TOut>) StaticGetters[memberName];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static TOut GetValue<TOut>(T target, string memberName) {
            return ((Func<T, TOut>) Getters[memberName])(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static TOut GetStaticValue<TOut>(string memberName) {
            return ((Func<TOut>) StaticGetters[memberName])();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        internal static void Preload(MemberInfo[] members) {
            lock (Getters) {
                for (int i = 0; i < members.Length; i++) {
                    PreloadGetter(Getters, GettersExpressions, members[i]);
                    PreloadToStringGetter(ToStringGetters, ToStringGettersExpressions, members[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        internal static void PreloadStatic(MemberInfo[] members) {
            lock (StaticGetters) {
                for (int i = 0; i < members.Length; i++) {
                    PreloadGetter(StaticGetters, StaticGettersExpressions, members[i]);
                    PreloadToStringGetter(StaticToStringGetters, StaticToStringGettersExpressions, members[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        internal static void PreloadGetter(Dictionary<string, Delegate> to, Dictionary<string, Expression> toExpr, MemberInfo member) {
            var parameter = Expression.Parameter(typeof(T), "obj");
            LambdaExpression lambda;
            if (member is PropertyInfo pi) {
                if (pi.GetMethod?.IsStatic ?? pi.SetMethod!.IsStatic)
                    lambda = Expression.Lambda(typeof(Func<>).MakeGenericType(pi.PropertyType),
                                               Expression.Property(null, pi));
                else
                    lambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(T), pi.PropertyType),
                                               Expression.Property(parameter, pi),
                                               new ParameterExpression[] { parameter });
            } else if (member is FieldInfo fi) {
                if (fi.IsStatic)
                    lambda = Expression.Lambda(typeof(Func<>).MakeGenericType(fi.FieldType),
                                               Expression.Field(null, fi));
                else
                    lambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(T), fi.FieldType),
                                               Expression.Field(parameter, fi),
                                               new ParameterExpression[] { parameter });
            } else {
                throw new InvalidOperationException("Member is not a property or a field.");
            }

            toExpr.Add(member.Name, lambda);
            to.Add(member.Name, lambda.Compile());
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        internal static void PreloadToStringGetter(Dictionary<string, Delegate> to, Dictionary<string, Expression> toExpr, MemberInfo member) {
            var pi = member as PropertyInfo;
            var fi = member as FieldInfo;
            var underlyingType = pi?.PropertyType ?? fi?.FieldType ?? throw new NotSupportedException();
            var parser = ConfigParsers.ResolveToStringConverter(underlyingType);
            if (parser == null) {
                return; //we cant use this member
            }

            ParameterExpression parameter = Expression.Parameter(typeof(T), "obj");
            LambdaExpression getter;            
            if (member is PropertyInfo) {
                if (pi.GetMethod?.IsStatic ?? pi.SetMethod!.IsStatic)
                    getter = Expression.Lambda(typeof(Func<>).MakeGenericType(typeof(string)),
                                               ExpressionHelper.ReplaceLambdaParameter(parser, parser.Parameters[0], Expression.Property(null, pi)).Body);
                else
                    getter = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(T), typeof(string)),
                                               ExpressionHelper.ReplaceLambdaParameter(parser, parser.Parameters[0], Expression.Property(parameter, pi)).Body,
                                               new ParameterExpression[] { parameter });
            } else if (member is FieldInfo) {
                if (fi.IsStatic)
                    getter = Expression.Lambda(typeof(Func<>).MakeGenericType(typeof(string)),
                                               ExpressionHelper.ReplaceLambdaParameter(parser, parser.Parameters[0], Expression.Field(null, fi)).Body);
                else
                    getter = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(T), typeof(string)),
                                               ExpressionHelper.ReplaceLambdaParameter(parser, parser.Parameters[0], Expression.Field(parameter, fi)).Body,
                                               new ParameterExpression[] { parameter });
            } else {
                throw new InvalidOperationException("Member is not a property or a field.");
            }
            
            toExpr.Add(member.Name, getter);
            to.Add(member.Name, getter.Compile());
        }
    }
}