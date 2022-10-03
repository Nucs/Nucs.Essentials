using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using EnumsNET;
using Nucs.Extensions;
using Nucs.Reflection;
using static Nucs.AbstractInfrastructure;
using Type = System.Type;

namespace Nucs.Configuration {
    public delegate T ConverterDelegate<out T>(ReadOnlySpan<char> str);
    public delegate string StringConverterDelegate<in T>(T str, char delimiter = ',', string? format = null);
    public delegate string BoxedStringConverterDelegate(object str, char delimiter = ',', string? format = null);

    public static class ConfigParsers {
        /// <summary>
        ///     A dictionary of <see cref="ConverterDelegate{T}"/> that parses ReadOnlySpan{char} to {T}    
        /// </summary>
        /// <remarks>Custom type handlers can be added here</remarks>
        public static Dictionary<Type, object> Converters = new() {
            { typeof(bool), new ConverterDelegate<bool>(c => bool.Parse(c)) },
            { typeof(int), new ConverterDelegate<int>(c => int.Parse(c)) }, {
                typeof(double), new ConverterDelegate<double>(c => {
                    if (c[^1] == '%')
                        return double.Parse(c.TrimEnd('%'), NumberStyles.Float | NumberStyles.AllowThousands) / 100d;

                    return double.Parse(c, NumberStyles.Float | NumberStyles.AllowThousands);
                })
            },
            { typeof(long), new ConverterDelegate<long>(c => long.Parse(c)) },
            { typeof(short), new ConverterDelegate<short>(c => short.Parse(c)) },
            { typeof(byte), new ConverterDelegate<byte>(c => byte.Parse(c)) },
            { typeof(uint), new ConverterDelegate<uint>(c => uint.Parse(c)) },
            { typeof(ushort), new ConverterDelegate<ushort>(c => ushort.Parse(c)) },
            { typeof(sbyte), new ConverterDelegate<sbyte>(c => sbyte.Parse(c)) },
            { typeof(float), new ConverterDelegate<float>(c => float.Parse(c)) },
            { typeof(decimal), new ConverterDelegate<decimal>(c => decimal.Parse(c)) },
            { typeof(string), new ConverterDelegate<string>(c => c.ToString()) },
            { typeof(DateTime), new ConverterDelegate<DateTime>(ConfigParsers.DateTime) },
            { typeof(TimeSpan), new ConverterDelegate<TimeSpan>(ConfigParsers.TimeSpan) },
            { typeof(bool?), new ConverterDelegate<bool?>(c => bool.Parse(c)) },
            { typeof(int?), new ConverterDelegate<int?>(c => int.Parse(c)) },
            { typeof(double?), new ConverterDelegate<double?>(c => double.Parse(c)) },
            { typeof(long?), new ConverterDelegate<long?>(c => long.Parse(c)) },
            { typeof(short?), new ConverterDelegate<short?>(c => short.Parse(c)) },
            { typeof(byte?), new ConverterDelegate<byte?>(c => byte.Parse(c)) },
            { typeof(uint?), new ConverterDelegate<uint?>(c => uint.Parse(c)) },
            { typeof(ushort?), new ConverterDelegate<ushort?>(c => ushort.Parse(c)) },
            { typeof(sbyte?), new ConverterDelegate<sbyte?>(c => sbyte.Parse(c)) },
            { typeof(float?), new ConverterDelegate<float?>(c => float.Parse(c)) },
            { typeof(decimal?), new ConverterDelegate<decimal?>(c => decimal.Parse(c)) },
            { typeof(DateTime?), new ConverterDelegate<DateTime?>(c => ConfigParsers.DateTime(c)) },
            { typeof(TimeSpan?), new ConverterDelegate<TimeSpan?>(c => ConfigParsers.TimeSpan(c)) },

            { typeof(int[]), new ConverterDelegate<int[]>(GetArrayOf<int>) },
            { typeof(double[]), new ConverterDelegate<double[]>(GetArrayOf<double>) },
            { typeof(bool[]), new ConverterDelegate<bool[]>(GetArrayOf<bool>) },
            { typeof(DateTime[]), new ConverterDelegate<DateTime[]>(GetArrayOf<DateTime>) },
            { typeof(TimeSpan[]), new ConverterDelegate<TimeSpan[]>(GetArrayOf<TimeSpan>) },
            { typeof(long[]), new ConverterDelegate<long[]>(GetArrayOf<long>) },
            { typeof(short[]), new ConverterDelegate<short[]>(GetArrayOf<short>) },
            { typeof(byte[]), new ConverterDelegate<byte[]>(GetArrayOf<byte>) },
            { typeof(uint[]), new ConverterDelegate<uint[]>(GetArrayOf<uint>) },
            { typeof(ushort[]), new ConverterDelegate<ushort[]>(GetArrayOf<ushort>) },
            { typeof(sbyte[]), new ConverterDelegate<sbyte[]>(GetArrayOf<sbyte>) },
            { typeof(float[]), new ConverterDelegate<float[]>(GetArrayOf<float>) },
            { typeof(decimal[]), new ConverterDelegate<decimal[]>(GetArrayOf<decimal>) },
            { typeof(string[]), new ConverterDelegate<string[]>(GetArrayOf<string>) },
            { typeof(bool?[]), new ConverterDelegate<bool?[]>(GetArrayOf<bool?>) },
            { typeof(int?[]), new ConverterDelegate<int?[]>(GetArrayOf<int?>) },
            { typeof(double?[]), new ConverterDelegate<double?[]>(GetArrayOf<double?>) },
            { typeof(long?[]), new ConverterDelegate<long?[]>(GetArrayOf<long?>) },
            { typeof(short?[]), new ConverterDelegate<short?[]>(GetArrayOf<short?>) },
            { typeof(byte?[]), new ConverterDelegate<byte?[]>(GetArrayOf<byte?>) },
            { typeof(uint?[]), new ConverterDelegate<uint?[]>(GetArrayOf<uint?>) },
            { typeof(ushort?[]), new ConverterDelegate<ushort?[]>(GetArrayOf<ushort?>) },
            { typeof(sbyte?[]), new ConverterDelegate<sbyte?[]>(GetArrayOf<sbyte?>) },
            { typeof(float?[]), new ConverterDelegate<float?[]>(GetArrayOf<float?>) },
            { typeof(decimal?[]), new ConverterDelegate<decimal?[]>(GetArrayOf<decimal?>) },
            { typeof(DateTime?[]), new ConverterDelegate<DateTime?[]>(GetArrayOf<DateTime?>) },
            { typeof(TimeSpan?[]), new ConverterDelegate<TimeSpan?[]>(GetArrayOf<TimeSpan?>) },

            { typeof(List<int>), new ConverterDelegate<List<int>>(GetListOf<int>) },
            { typeof(List<double>), new ConverterDelegate<List<double>>(GetListOf<double>) },
            { typeof(List<bool>), new ConverterDelegate<List<bool>>(GetListOf<bool>) },
            { typeof(List<DateTime>), new ConverterDelegate<List<DateTime>>(GetListOf<DateTime>) },
            { typeof(List<TimeSpan>), new ConverterDelegate<List<TimeSpan>>(GetListOf<TimeSpan>) },
            { typeof(List<long>), new ConverterDelegate<List<long>>(GetListOf<long>) },
            { typeof(List<short>), new ConverterDelegate<List<short>>(GetListOf<short>) },
            { typeof(List<byte>), new ConverterDelegate<List<byte>>(GetListOf<byte>) },
            { typeof(List<uint>), new ConverterDelegate<List<uint>>(GetListOf<uint>) },
            { typeof(List<ushort>), new ConverterDelegate<List<ushort>>(GetListOf<ushort>) },
            { typeof(List<sbyte>), new ConverterDelegate<List<sbyte>>(GetListOf<sbyte>) },
            { typeof(List<float>), new ConverterDelegate<List<float>>(GetListOf<float>) },
            { typeof(List<decimal>), new ConverterDelegate<List<decimal>>(GetListOf<decimal>) },
            { typeof(List<string>), new ConverterDelegate<List<string>>(GetListOf<string>) },
            { typeof(List<bool?>), new ConverterDelegate<List<bool?>>(GetListOf<bool?>) },
            { typeof(List<int?>), new ConverterDelegate<List<int?>>(GetListOf<int?>) },
            { typeof(List<double?>), new ConverterDelegate<List<double?>>(GetListOf<double?>) },
            { typeof(List<long?>), new ConverterDelegate<List<long?>>(GetListOf<long?>) },
            { typeof(List<short?>), new ConverterDelegate<List<short?>>(GetListOf<short?>) },
            { typeof(List<byte?>), new ConverterDelegate<List<byte?>>(GetListOf<byte?>) },
            { typeof(List<uint?>), new ConverterDelegate<List<uint?>>(GetListOf<uint?>) },
            { typeof(List<ushort?>), new ConverterDelegate<List<ushort?>>(GetListOf<ushort?>) },
            { typeof(List<sbyte?>), new ConverterDelegate<List<sbyte?>>(GetListOf<sbyte?>) },
            { typeof(List<float?>), new ConverterDelegate<List<float?>>(GetListOf<float?>) },
            { typeof(List<decimal?>), new ConverterDelegate<List<decimal?>>(GetListOf<decimal?>) },
            { typeof(List<DateTime?>), new ConverterDelegate<List<DateTime?>>(GetListOf<DateTime?>) },
            { typeof(List<TimeSpan?>), new ConverterDelegate<List<TimeSpan?>>(GetListOf<TimeSpan?>) },
        };

        public static List<T> GetListOf<T>(ReadOnlySpan<char> Value) {
            if (Value.IsEmpty)
                return new List<T>(0);
            var line = new LineReader(Value);
            var result = new List<T>(line.CountItems());
            var converter = (ConverterDelegate<T>) ConfigParsers.Converters[typeof(T)];
            while (line.HasNext)
                result.Add(converter(line.Next()));
            return result;
        }

        public static T[] GetArrayOf<T>(ReadOnlySpan<char> Value) {
            if (Value.IsEmpty)
                return Array.Empty<T>();
            var line = new LineReader(Value);
            var result = new T[line.CountItems()];
            var converter = (ConverterDelegate<T>) ConfigParsers.Converters[typeof(T)];
            for (int i = 0; line.HasNext; i++) {
                result[i] = converter(line.Next());
            }

            return result;
        }


        /// <summary>
        ///     A dictionary of <see cref="ConverterDelegate{T}"/> that parses ReadOnlySpan{char} to {T}    
        /// </summary>
        /// <remarks>Custom type handlers can be added here</remarks>
        public static Dictionary<Type, ConverterDelegate<object>> BoxedConverters = new Dictionary<Type, ConverterDelegate<object>> {
            { typeof(bool), c => bool.Parse(c) },
            { typeof(int), c => int.Parse(c) }, {
                typeof(double), c => {
                    if (c[^1] == '%')
                        return double.Parse(c.TrimEnd('%'), NumberStyles.Float | NumberStyles.AllowThousands) / 100d;

                    return double.Parse(c, NumberStyles.Float | NumberStyles.AllowThousands);
                }
            },
            { typeof(long), c => long.Parse(c) },
            { typeof(short), c => short.Parse(c) },
            { typeof(byte), c => byte.Parse(c) },
            { typeof(uint), c => uint.Parse(c) },
            { typeof(ushort), c => ushort.Parse(c) },
            { typeof(sbyte), c => sbyte.Parse(c) },
            { typeof(float), c => float.Parse(c, NumberStyles.Float | NumberStyles.AllowThousands) },
            { typeof(decimal), c => decimal.Parse(c) },
            { typeof(string), c => c.ToString() },
            { typeof(DateTime), str => ConfigParsers.DateTime(str) },
            { typeof(TimeSpan), str => ConfigParsers.TimeSpan(str) },
            { typeof(bool?), c => bool.Parse(c) },
            { typeof(int?), c => int.Parse(c) },
            { typeof(double?), c => double.Parse(c) },
            { typeof(long?), c => long.Parse(c) },
            { typeof(short?), c => short.Parse(c) },
            { typeof(byte?), c => byte.Parse(c) },
            { typeof(uint?), c => uint.Parse(c) },
            { typeof(ushort?), c => ushort.Parse(c) },
            { typeof(sbyte?), c => sbyte.Parse(c) },
            { typeof(float?), c => float.Parse(c) },
            { typeof(decimal?), c => decimal.Parse(c) },
            { typeof(DateTime?), str => ConfigParsers.DateTime(str) },
            { typeof(TimeSpan?), str => ConfigParsers.TimeSpan(str) },

            { typeof(int[]), GetArrayOf<int> },
            { typeof(double[]), GetArrayOf<double> },
            { typeof(bool[]), GetArrayOf<bool> },
            { typeof(DateTime[]), GetArrayOf<DateTime> },
            { typeof(TimeSpan[]), GetArrayOf<TimeSpan> },
            { typeof(long[]), GetArrayOf<long> },
            { typeof(short[]), GetArrayOf<short> },
            { typeof(byte[]), GetArrayOf<byte> },
            { typeof(uint[]), GetArrayOf<uint> },
            { typeof(ushort[]), GetArrayOf<ushort> },
            { typeof(sbyte[]), GetArrayOf<sbyte> },
            { typeof(float[]), GetArrayOf<float> },
            { typeof(decimal[]), GetArrayOf<decimal> },
            { typeof(string[]), GetArrayOf<string> },
            { typeof(bool?[]), GetArrayOf<bool?> },
            { typeof(int?[]), GetArrayOf<int?> },
            { typeof(double?[]), GetArrayOf<double?> },
            { typeof(long?[]), GetArrayOf<long?> },
            { typeof(short?[]), GetArrayOf<short?> },
            { typeof(byte?[]), GetArrayOf<byte?> },
            { typeof(uint?[]), GetArrayOf<uint?> },
            { typeof(ushort?[]), GetArrayOf<ushort?> },
            { typeof(sbyte?[]), GetArrayOf<sbyte?> },
            { typeof(float?[]), GetArrayOf<float?> },
            { typeof(decimal?[]), GetArrayOf<decimal?> },
            { typeof(DateTime?[]), GetArrayOf<DateTime?> },
            { typeof(TimeSpan?[]), GetArrayOf<TimeSpan?> },

            { typeof(List<int>), GetListOf<int> },
            { typeof(List<double>), GetListOf<double> },
            { typeof(List<bool>), GetListOf<bool> },
            { typeof(List<DateTime>), GetListOf<DateTime> },
            { typeof(List<TimeSpan>), GetListOf<TimeSpan> },
            { typeof(List<long>), GetListOf<long> },
            { typeof(List<short>), GetListOf<short> },
            { typeof(List<byte>), GetListOf<byte> },
            { typeof(List<uint>), GetListOf<uint> },
            { typeof(List<ushort>), GetListOf<ushort> },
            { typeof(List<sbyte>), GetListOf<sbyte> },
            { typeof(List<float>), GetListOf<float> },
            { typeof(List<decimal>), GetListOf<decimal> },
            { typeof(List<string>), GetListOf<string> },
            { typeof(List<bool?>), GetListOf<bool?> },
            { typeof(List<int?>), GetListOf<int?> },
            { typeof(List<double?>), GetListOf<double?> },
            { typeof(List<long?>), GetListOf<long?> },
            { typeof(List<short?>), GetListOf<short?> },
            { typeof(List<byte?>), GetListOf<byte?> },
            { typeof(List<uint?>), GetListOf<uint?> },
            { typeof(List<ushort?>), GetListOf<ushort?> },
            { typeof(List<sbyte?>), GetListOf<sbyte?> },
            { typeof(List<float?>), GetListOf<float?> },
            { typeof(List<decimal?>), GetListOf<decimal?> },
            { typeof(List<DateTime?>), GetListOf<DateTime?> },
            { typeof(List<TimeSpan?>), GetListOf<TimeSpan?> },
        };

        public static Dictionary<Type, BoxedStringConverterDelegate> BoxedToStringConverters = new() {
            { typeof(bool), (c, delimiter, format) => c.ToString() },
            { typeof(int), (c, delimiter, format) => c.ToString() },
            { typeof(double), (c, delimiter, format) => c.ToString() },
            { typeof(long), (c, delimiter, format) => c.ToString() },
            { typeof(short), (c, delimiter, format) => c.ToString() },
            { typeof(byte), (c, delimiter, format) => c.ToString() },
            { typeof(uint), (c, delimiter, format) => c.ToString() },
            { typeof(ushort), (c, delimiter, format) => c.ToString() },
            { typeof(sbyte), (c, delimiter, format) => c.ToString() },
            { typeof(float), (c, delimiter, format) => c.ToString() },
            { typeof(decimal), (c, delimiter, format) => c.ToString() },
            { typeof(string), (c, delimiter, format) => (string) c },
            { typeof(DateTime), (c, delimiter, format) => c.ToString() },
            { typeof(TimeSpan), (c, delimiter, format) => c.ToString() },
            { typeof(bool?), (c, delimiter, format) => c.ToString() },
            { typeof(int?), (c, delimiter, format) => c.ToString() },
            { typeof(double?), (c, delimiter, format) => c.ToString() },
            { typeof(long?), (c, delimiter, format) => c.ToString() },
            { typeof(short?), (c, delimiter, format) => c.ToString() },
            { typeof(byte?), (c, delimiter, format) => c.ToString() },
            { typeof(uint?), (c, delimiter, format) => c.ToString() },
            { typeof(ushort?), (c, delimiter, format) => c.ToString() },
            { typeof(sbyte?), (c, delimiter, format) => c.ToString() },
            { typeof(float?), (c, delimiter, format) => c.ToString() },
            { typeof(decimal?), (c, delimiter, format) => c.ToString() },
            { typeof(DateTime?), (c, delimiter, format) => c.ToString() },
            { typeof(TimeSpan?), (c, delimiter, format) => c.ToString() },

            { typeof(bool[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(int[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(double[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(long[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(short[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(byte[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(uint[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(ushort[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(sbyte[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(float[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(decimal[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(string[]), (c, delimiter, format) => string.Join(",", c) },
            { typeof(DateTime[]), (c, delimiter, format) => string.Join(",", ((List<DateTime>) c).Select(dt => dt.ToString(format))) },
            { typeof(TimeSpan[]), (c, delimiter, format) => string.Join(",", ((List<TimeSpan>) c).Select(ts => ts.ToString(format))) },

            { typeof(List<bool>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<int>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<double>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<long>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<short>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<byte>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<uint>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<ushort>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<sbyte>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<float>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<decimal>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<string>), (c, delimiter, format) => string.Join(",", c) },
            { typeof(List<DateTime>), (c, delimiter, format) => string.Join(",", ((List<DateTime>) c).Select(dt => dt.ToString(format))) },
            { typeof(List<TimeSpan>), (c, delimiter, format) => string.Join(",", ((List<TimeSpan>) c).Select(ts => ts.ToString(format))) },
        };

        public static Dictionary<Type, object> ToStringConverters = new() {
            { typeof(bool), new StringConverterDelegate<bool>((c, delimiter, format) => c.ToString()) },
            { typeof(int), new StringConverterDelegate<int>((c, delimiter, format) => c.ToString(format)) },
            { typeof(double), new StringConverterDelegate<double>((c, delimiter, format) => c.ToString(format)) },
            { typeof(long), new StringConverterDelegate<long>((c, delimiter, format) => c.ToString(format)) },
            { typeof(short), new StringConverterDelegate<short>((c, delimiter, format) => c.ToString(format)) },
            { typeof(byte), new StringConverterDelegate<byte>((c, delimiter, format) => c.ToString(format)) },
            { typeof(uint), new StringConverterDelegate<uint>((c, delimiter, format) => c.ToString(format)) },
            { typeof(ushort), new StringConverterDelegate<ushort>((c, delimiter, format) => c.ToString(format)) },
            { typeof(sbyte), new StringConverterDelegate<sbyte>((c, delimiter, format) => c.ToString(format)) },
            { typeof(float), new StringConverterDelegate<float>((c, delimiter, format) => c.ToString(format)) },
            { typeof(decimal), new StringConverterDelegate<decimal>((c, delimiter, format) => c.ToString(format)) },
            { typeof(string), new StringConverterDelegate<string>((c, delimiter, format) => c) },
            { typeof(DateTime), new StringConverterDelegate<DateTime>((c, delimiter, format) => c.ToString(format)) },
            { typeof(TimeSpan), new StringConverterDelegate<TimeSpan>((c, delimiter, format) => c.ToString(format)) },
            { typeof(bool?), new StringConverterDelegate<bool?>((c, delimiter, format) => c?.ToString()) },
            { typeof(int?), new StringConverterDelegate<int?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(double?), new StringConverterDelegate<double?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(long?), new StringConverterDelegate<long?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(short?), new StringConverterDelegate<short?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(byte?), new StringConverterDelegate<byte?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(uint?), new StringConverterDelegate<uint?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(ushort?), new StringConverterDelegate<ushort?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(sbyte?), new StringConverterDelegate<sbyte?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(float?), new StringConverterDelegate<float?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(decimal?), new StringConverterDelegate<decimal?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(DateTime?), new StringConverterDelegate<DateTime?>((c, delimiter, format) => c?.ToString(format)) },
            { typeof(TimeSpan?), new StringConverterDelegate<TimeSpan?>((c, delimiter, format) => c?.ToString(format)) },

            { typeof(List<bool>), new StringConverterDelegate<List<bool>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<int>), new StringConverterDelegate<List<int>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<double>), new StringConverterDelegate<List<double>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<long>), new StringConverterDelegate<List<long>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<short>), new StringConverterDelegate<List<short>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<byte>), new StringConverterDelegate<List<byte>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<uint>), new StringConverterDelegate<List<uint>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<ushort>), new StringConverterDelegate<List<ushort>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<sbyte>), new StringConverterDelegate<List<sbyte>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<float>), new StringConverterDelegate<List<float>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<decimal>), new StringConverterDelegate<List<decimal>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<string>), new StringConverterDelegate<List<string>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(List<DateTime>), new StringConverterDelegate<List<DateTime>>((c, delimiter, format) => string.Join(",", c.Select(dt => dt.ToString(format)))) },
            { typeof(List<TimeSpan>), new StringConverterDelegate<List<TimeSpan>>((c, delimiter, format) => string.Join(",", c.Select(ts => ts.ToString(format)))) },

            { typeof(bool[]), new StringConverterDelegate<List<bool>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(int[]), new StringConverterDelegate<List<int>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(double[]), new StringConverterDelegate<List<double>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(long[]), new StringConverterDelegate<List<long>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(short[]), new StringConverterDelegate<List<short>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(byte[]), new StringConverterDelegate<List<byte>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(uint[]), new StringConverterDelegate<List<uint>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(ushort[]), new StringConverterDelegate<List<ushort>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(sbyte[]), new StringConverterDelegate<List<sbyte>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(float[]), new StringConverterDelegate<List<float>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(decimal[]), new StringConverterDelegate<List<decimal>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(string[]), new StringConverterDelegate<List<string>>((c, delimiter, format) => string.Join(",", c)) },
            { typeof(DateTime[]), new StringConverterDelegate<List<DateTime>>((c, delimiter, format) => string.Join(",", c.Select(dt => dt.ToString(format)))) },
            { typeof(TimeSpan[]), new StringConverterDelegate<List<TimeSpan>>((c, delimiter, format) => string.Join(",", c.Select(ts => ts.ToString(format)))) },
        };

        private static Expression<Func<T, string>> Expr<T>(Expression<Func<T, string>> func) {
            return func;
        }

        public static LambdaExpression? ResolveToStringConverter(Type targetType) {
            if (ConfigParsers.ExpressionToStringConverters.TryGetValue(targetType, out LambdaExpression? parser))
                return parser;

            //todo is enum or list of enums?
            bool isList = false;
            var underlyingType = targetType.IsEnum
                ? targetType
                : (isList = targetType.HasElementType)
                    ? targetType.GetElementType()
                    : (isList = (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>)))
                        ? targetType.GetGenericArguments()[0]
                        : targetType;

            /*if (underlyingType == null) {
                return null;
            }*/

            _ = ConfigParsers.ExpressionToStringConverters.TryGetValue(underlyingType, out parser);

            bool isEnum = underlyingType.IsEnum;
            if (isEnum) {
                var parameter = Expression.Parameter(targetType);
                var asString = typeof(Enums).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(s => s.Name == ("AsString") && s.IsGenericMethod && s.GetGenericArguments().Length == 1 && s.GetParameters().Length == 1);

                if (!isList) {
                    return Expression.Lambda(Expression.Call(null, asString.MakeGenericMethod(underlyingType), parameter), parameter);
                } else {
                    var join = typeof(string).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(s => s.Name == ("Join") && s.GetParameters()[0].ParameterType == typeof(char) && s.GetParameters().Length == 2 && s.GetParameters()[1].ParameterType.IsGenericType);
                    return Expression.Lambda(Expression.Call(null, join.MakeGenericMethod(underlyingType), Expression.Constant(',', typeof(char)), parameter), parameter);
                }
            } else if (underlyingType.IsValueType) {
                var parameter = Expression.Parameter(targetType);
                if (!isList) {
                    return Expression.Lambda(Expression.Call(parameter, parameter.Type.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, Array.Empty<Type>(), null)), parameter);
                } else {
                    var join = typeof(string).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(s => s.Name == ("Join") && s.GetParameters()[0].ParameterType == typeof(char) && s.GetParameters().Length == 2 && s.GetParameters()[1].ParameterType.IsGenericType);
                    return Expression.Lambda(Expression.Call(null, join.MakeGenericMethod(underlyingType), Expression.Constant(',', typeof(char)), parameter), parameter);
                }
            } else {
                var parameter = Expression.Parameter(targetType);
                return Expression.Lambda(Expression.Call(parameter, typeof(object).GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, Array.Empty<Type>(), null)), parameter);
            }

            return null; //not supported
        }

        public static Dictionary<Type, LambdaExpression> ExpressionToStringConverters = new() {
            { typeof(bool), Expr<bool>(c => c.ToString()) },
            { typeof(int), Expr<int>(c => c.ToString()) },
            { typeof(double), Expr<double>(c => c.ToString(CultureInfo.InvariantCulture)) },
            { typeof(long), Expr<long>(c => c.ToString()) },
            { typeof(short), Expr<short>(c => c.ToString()) },
            { typeof(byte), Expr<byte>(c => c.ToString()) },
            { typeof(uint), Expr<uint>(c => c.ToString()) },
            { typeof(ushort), Expr<ushort>(c => c.ToString()) },
            { typeof(sbyte), Expr<sbyte>(c => c.ToString()) },
            { typeof(float), Expr<float>(c => c.ToString(CultureInfo.InvariantCulture)) },
            { typeof(decimal), Expr<decimal>(c => c.ToString(CultureInfo.InvariantCulture)) },
            { typeof(string), Expr<string>(c => c) },
            { typeof(DateTime), Expr<DateTime>(c => c.ToString(AbstractInfrastructure.DateTimeLongFormat)) },
            { typeof(TimeSpan), Expr<TimeSpan>(c => c.ToString(AbstractInfrastructure.TimeFormat)) },
            { typeof(bool?), Expr<bool?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(int?), Expr<int?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(double?), Expr<double?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(long?), Expr<long?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(short?), Expr<short?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(byte?), Expr<byte?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(uint?), Expr<uint?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(ushort?), Expr<ushort?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(sbyte?), Expr<sbyte?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(float?), Expr<float?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(decimal?), Expr<decimal?>(c => c.HasValue ? c.Value.ToString() : string.Empty) },
            { typeof(DateTime?), Expr<DateTime?>(c => c.HasValue ? c.Value.ToString(AbstractInfrastructure.DateTimeLongFormat) : string.Empty) },
            { typeof(TimeSpan?), Expr<TimeSpan?>(c => c.HasValue ? c.Value.ToString(AbstractInfrastructure.TimeFormat) : string.Empty) },

            { typeof(PooledStrongBox<bool>), Expr<PooledStrongBox<bool>>(c => c.Value.ToString()) },
            { typeof(PooledStrongBox<int>), Expr<PooledStrongBox<int>>(c => c.Value.ToString()) },
            { typeof(PooledStrongBox<double>), Expr<PooledStrongBox<double>>(c => c.Value.ToString(CultureInfo.InvariantCulture)) },
            { typeof(PooledStrongBox<long>), Expr<PooledStrongBox<long>>(c => c.Value.ToString()) },
            { typeof(PooledStrongBox<short>), Expr<PooledStrongBox<short>>(c => c.Value.ToString()) },
            { typeof(PooledStrongBox<byte>), Expr<PooledStrongBox<byte>>(c => c.Value.ToString()) },
            { typeof(PooledStrongBox<uint>), Expr<PooledStrongBox<uint>>(c => c.Value.ToString()) },
            { typeof(PooledStrongBox<ushort>), Expr<PooledStrongBox<ushort>>(c => c.Value.ToString()) },
            { typeof(PooledStrongBox<sbyte>), Expr<PooledStrongBox<sbyte>>(c => c.Value.ToString()) },
            { typeof(PooledStrongBox<float>), Expr<PooledStrongBox<float>>(c => c.Value.ToString(CultureInfo.InvariantCulture)) },
            { typeof(PooledStrongBox<decimal>), Expr<PooledStrongBox<decimal>>(c => c.Value.ToString(CultureInfo.InvariantCulture)) },
            { typeof(PooledStrongBox<string>), Expr<PooledStrongBox<string>>(c => c) },
            { typeof(PooledStrongBox<DateTime>), Expr<PooledStrongBox<DateTime>>(c => c.Value.ToString(AbstractInfrastructure.DateTimeLongFormat)) },
            { typeof(PooledStrongBox<TimeSpan>), Expr<PooledStrongBox<TimeSpan>>(c => c.Value.ToString(AbstractInfrastructure.TimeFormat)) },
            { typeof(PooledStrongBox<bool?>), Expr<PooledStrongBox<bool?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(PooledStrongBox<int?>), Expr<PooledStrongBox<int?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(PooledStrongBox<double?>), Expr<PooledStrongBox<double?>>(c => c.Value.HasValue ? c.Value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) },
            { typeof(PooledStrongBox<long?>), Expr<PooledStrongBox<long?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(PooledStrongBox<short?>), Expr<PooledStrongBox<short?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(PooledStrongBox<byte?>), Expr<PooledStrongBox<byte?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(PooledStrongBox<uint?>), Expr<PooledStrongBox<uint?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(PooledStrongBox<ushort?>), Expr<PooledStrongBox<ushort?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(PooledStrongBox<sbyte?>), Expr<PooledStrongBox<sbyte?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(PooledStrongBox<float?>), Expr<PooledStrongBox<float?>>(c => c.Value.HasValue ? c.Value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) },
            { typeof(PooledStrongBox<decimal?>), Expr<PooledStrongBox<decimal?>>(c => c.Value.HasValue ? c.Value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) },
            { typeof(PooledStrongBox<DateTime?>), Expr<PooledStrongBox<DateTime?>>(c => c.Value.HasValue ? c.Value.Value.ToString(AbstractInfrastructure.DateTimeLongFormat) : string.Empty) },
            { typeof(PooledStrongBox<TimeSpan?>), Expr<PooledStrongBox<TimeSpan?>>(c => c.Value.HasValue ? c.Value.Value.ToString(AbstractInfrastructure.TimeFormat) : string.Empty) },

            { typeof(List<bool>), Expr<List<bool>>((c) => string.Join(",", c)) },
            { typeof(List<int>), Expr<List<int>>((c) => string.Join(",", c)) },
            { typeof(List<double>), Expr<List<double>>((c) => string.Join(",", c)) },
            { typeof(List<long>), Expr<List<long>>((c) => string.Join(",", c)) },
            { typeof(List<short>), Expr<List<short>>((c) => string.Join(",", c)) },
            { typeof(List<byte>), Expr<List<byte>>((c) => string.Join(",", c)) },
            { typeof(List<uint>), Expr<List<uint>>((c) => string.Join(",", c)) },
            { typeof(List<ushort>), Expr<List<ushort>>((c) => string.Join(",", c)) },
            { typeof(List<sbyte>), Expr<List<sbyte>>((c) => string.Join(",", c)) },
            { typeof(List<float>), Expr<List<float>>((c) => string.Join(",", c)) },
            { typeof(List<decimal>), Expr<List<decimal>>((c) => string.Join(",", c)) },
            { typeof(List<string>), Expr<List<string>>((c) => string.Join(",", c)) },
            { typeof(List<DateTime>), Expr<List<DateTime>>((c) => string.Join(",", c.Select(dt => dt.ToString(AbstractInfrastructure.DateTimeLongFormat)))) },
            { typeof(List<TimeSpan>), Expr<List<TimeSpan>>((c) => string.Join(",", c.Select(ts => ts.ToString(AbstractInfrastructure.TimeFormat)))) },

            { typeof(bool[]), Expr<List<bool>>((c) => string.Join(",", c)) },
            { typeof(int[]), Expr<List<int>>((c) => string.Join(",", c)) },
            { typeof(double[]), Expr<List<double>>((c) => string.Join(",", c)) },
            { typeof(long[]), Expr<List<long>>((c) => string.Join(",", c)) },
            { typeof(short[]), Expr<List<short>>((c) => string.Join(",", c)) },
            { typeof(byte[]), Expr<List<byte>>((c) => string.Join(",", c)) },
            { typeof(uint[]), Expr<List<uint>>((c) => string.Join(",", c)) },
            { typeof(ushort[]), Expr<List<ushort>>((c) => string.Join(",", c)) },
            { typeof(sbyte[]), Expr<List<sbyte>>((c) => string.Join(",", c)) },
            { typeof(float[]), Expr<List<float>>((c) => string.Join(",", c)) },
            { typeof(decimal[]), Expr<List<decimal>>((c) => string.Join(",", c)) },
            { typeof(string[]), Expr<List<string>>((c) => string.Join(",", c)) },
            { typeof(DateTime[]), Expr<List<DateTime>>((c) => string.Join(",", c.Select(dt => dt.ToString(AbstractInfrastructure.DateTimeLongFormat)))) },
            { typeof(TimeSpan[]), Expr<List<TimeSpan>>((c) => string.Join(",", c.Select(ts => ts.ToString(AbstractInfrastructure.TimeFormat)))) },
        };

        private static Expression<Func<object, string>> ExprBoxed<T>(Expression<Func<T, string>> func) where T : class {
            Expression<Func<object, T>> converter = o => Unsafe.As<T>(o);
            var mixedBoxy = ExpressionHelper.ReplaceLambdaParameter(func, func.Parameters[0], converter.Body).Body;
            return Expression.Lambda<Func<object, string>>(mixedBoxy, converter.Parameters);
        }

        public static Dictionary<Type, Expression<Func<object, string>>> BoxedExprBoxedToStringConverters = new() {
            { typeof(bool), ExprBoxed<PooledStrongBox<bool>>(c => c.Value.ToString()) },
            { typeof(int), ExprBoxed<PooledStrongBox<int>>(c => c.Value.ToString()) },
            { typeof(double), ExprBoxed<PooledStrongBox<double>>(c => c.Value.ToString(CultureInfo.InvariantCulture)) },
            { typeof(long), ExprBoxed<PooledStrongBox<long>>(c => c.Value.ToString()) },
            { typeof(short), ExprBoxed<PooledStrongBox<short>>(c => c.Value.ToString()) },
            { typeof(byte), ExprBoxed<PooledStrongBox<byte>>(c => c.Value.ToString()) },
            { typeof(uint), ExprBoxed<PooledStrongBox<uint>>(c => c.Value.ToString()) },
            { typeof(ushort), ExprBoxed<PooledStrongBox<ushort>>(c => c.Value.ToString()) },
            { typeof(sbyte), ExprBoxed<PooledStrongBox<sbyte>>(c => c.Value.ToString()) },
            { typeof(float), ExprBoxed<PooledStrongBox<float>>(c => c.Value.ToString(CultureInfo.InvariantCulture)) },
            { typeof(decimal), ExprBoxed<PooledStrongBox<decimal>>(c => c.Value.ToString(CultureInfo.InvariantCulture)) },
            { typeof(string), ExprBoxed<string?>(c => c ?? string.Empty) },
            { typeof(DateTime), ExprBoxed<PooledStrongBox<DateTime>>(c => c.Value.ToString(AbstractInfrastructure.DateTimeLongFormat)) },
            { typeof(TimeSpan), ExprBoxed<PooledStrongBox<TimeSpan>>(c => c.Value.ToString(AbstractInfrastructure.TimeFormat)) },
            { typeof(bool?), ExprBoxed<PooledStrongBox<bool?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(int?), ExprBoxed<PooledStrongBox<int?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(double?), ExprBoxed<PooledStrongBox<double?>>(c => c.Value.HasValue ? c.Value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) },
            { typeof(long?), ExprBoxed<PooledStrongBox<long?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(short?), ExprBoxed<PooledStrongBox<short?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(byte?), ExprBoxed<PooledStrongBox<byte?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(uint?), ExprBoxed<PooledStrongBox<uint?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(ushort?), ExprBoxed<PooledStrongBox<ushort?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(sbyte?), ExprBoxed<PooledStrongBox<sbyte?>>(c => c.Value.HasValue ? c.Value.Value.ToString() : string.Empty) },
            { typeof(float?), ExprBoxed<PooledStrongBox<float?>>(c => c.Value.HasValue ? c.Value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) },
            { typeof(decimal?), ExprBoxed<PooledStrongBox<decimal?>>(c => c.Value.HasValue ? c.Value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) },
            { typeof(DateTime?), ExprBoxed<PooledStrongBox<DateTime?>>(c => c.Value.HasValue ? c.Value.Value.ToString(AbstractInfrastructure.DateTimeLongFormat) : string.Empty) },
            { typeof(TimeSpan?), ExprBoxed<PooledStrongBox<TimeSpan?>>(c => c.Value.HasValue ? c.Value.Value.ToString(AbstractInfrastructure.TimeFormat) : string.Empty) },

            { typeof(List<bool>), ExprBoxed<List<bool>>((c) => string.Join(",", c)) },
            { typeof(List<int>), ExprBoxed<List<int>>((c) => string.Join(",", c)) },
            { typeof(List<double>), ExprBoxed<List<double>>((c) => string.Join(",", c)) },
            { typeof(List<long>), ExprBoxed<List<long>>((c) => string.Join(",", c)) },
            { typeof(List<short>), ExprBoxed<List<short>>((c) => string.Join(",", c)) },
            { typeof(List<byte>), ExprBoxed<List<byte>>((c) => string.Join(",", c)) },
            { typeof(List<uint>), ExprBoxed<List<uint>>((c) => string.Join(",", c)) },
            { typeof(List<ushort>), ExprBoxed<List<ushort>>((c) => string.Join(",", c)) },
            { typeof(List<sbyte>), ExprBoxed<List<sbyte>>((c) => string.Join(",", c)) },
            { typeof(List<float>), ExprBoxed<List<float>>((c) => string.Join(",", c)) },
            { typeof(List<decimal>), ExprBoxed<List<decimal>>((c) => string.Join(",", c)) },
            { typeof(List<string>), ExprBoxed<List<string>>((c) => string.Join(",", c)) },
            { typeof(List<DateTime>), ExprBoxed<List<DateTime>>((c) => string.Join(",", c.Select(dt => dt.ToString(AbstractInfrastructure.DateTimeLongFormat)))) },
            { typeof(List<TimeSpan>), ExprBoxed<List<TimeSpan>>((c) => string.Join(",", c.Select(ts => ts.ToString(AbstractInfrastructure.TimeFormat)))) },

            { typeof(bool[]), ExprBoxed<List<bool>>((c) => string.Join(",", c)) },
            { typeof(int[]), ExprBoxed<List<int>>((c) => string.Join(",", c)) },
            { typeof(double[]), ExprBoxed<List<double>>((c) => string.Join(",", c)) },
            { typeof(long[]), ExprBoxed<List<long>>((c) => string.Join(",", c)) },
            { typeof(short[]), ExprBoxed<List<short>>((c) => string.Join(",", c)) },
            { typeof(byte[]), ExprBoxed<List<byte>>((c) => string.Join(",", c)) },
            { typeof(uint[]), ExprBoxed<List<uint>>((c) => string.Join(",", c)) },
            { typeof(ushort[]), ExprBoxed<List<ushort>>((c) => string.Join(",", c)) },
            { typeof(sbyte[]), ExprBoxed<List<sbyte>>((c) => string.Join(",", c)) },
            { typeof(float[]), ExprBoxed<List<float>>((c) => string.Join(",", c)) },
            { typeof(decimal[]), ExprBoxed<List<decimal>>((c) => string.Join(",", c)) },
            { typeof(string[]), ExprBoxed<List<string>>((c) => string.Join(",", c)) },
            { typeof(DateTime[]), ExprBoxed<List<DateTime>>((c) => string.Join(",", c.Select(dt => dt.ToString(AbstractInfrastructure.DateTimeLongFormat)))) },
            { typeof(TimeSpan[]), ExprBoxed<List<TimeSpan>>((c) => string.Join(",", c.Select(ts => ts.ToString(AbstractInfrastructure.TimeFormat)))) },
        };

        public static Dictionary<string, Type> StringToType = new() {
            { "bool", typeof(bool) },
            { "int", typeof(int) },
            { "double", typeof(double) },
            { "long", typeof(long) },
            { "short", typeof(short) },
            { "byte", typeof(byte) },
            { "uint", typeof(uint) },
            { "ushort", typeof(ushort) },
            { "sbyte", typeof(sbyte) },
            { "float", typeof(float) },
            { "decimal", typeof(decimal) },
            { "string", typeof(string) },
            { "DateTime", typeof(DateTime) },
            { "TimeSpan", typeof(TimeSpan) },
            { "bool?", typeof(bool?) },
            { "int?", typeof(int?) },
            { "double?", typeof(double?) },
            { "long?", typeof(long?) },
            { "short?", typeof(short?) },
            { "byte?", typeof(byte?) },
            { "uint?", typeof(uint?) },
            { "ushort?", typeof(ushort?) },
            { "sbyte?", typeof(sbyte?) },
            { "float?", typeof(float?) },
            { "decimal?", typeof(decimal?) },
            { "DateTime?", typeof(DateTime?) },
            { "TimeSpan?", typeof(TimeSpan?) },
            { "List<bool>", typeof(List<bool>) },
            { "List<int>", typeof(List<int>) },
            { "List<double>", typeof(List<double>) },
            { "List<long>", typeof(List<long>) },
            { "List<short>", typeof(List<short>) },
            { "List<byte>", typeof(List<byte>) },
            { "List<uint>", typeof(List<uint>) },
            { "List<ushort>", typeof(List<ushort>) },
            { "List<sbyte>", typeof(List<sbyte>) },
            { "List<float>", typeof(List<float>) },
            { "List<decimal>", typeof(List<decimal>) },
            { "List<string>", typeof(List<string>) },
            { "List<DateTime>", typeof(List<DateTime>) },
            { "List<TimeSpan>", typeof(List<TimeSpan>) },
            { "bool[]", typeof(bool[]) },
            { "int[]", typeof(int[]) },
            { "double[]", typeof(double[]) },
            { "long[]", typeof(long[]) },
            { "short[]", typeof(short[]) },
            { "byte[]", typeof(byte[]) },
            { "uint[]", typeof(uint[]) },
            { "ushort[]", typeof(ushort[]) },
            { "sbyte[]", typeof(sbyte[]) },
            { "float[]", typeof(float[]) },
            { "decimal[]", typeof(decimal[]) },
            { "string[]", typeof(string[]) },
            { "DateTime[]", typeof(DateTime[]) },
            { "TimeSpan[]", typeof(TimeSpan[]) },
        };

        /// <summary>
        ///     A dictionary of <see cref="ConverterDelegate{T?}"/> that parses ReadOnlySpan{char} to {T?} as nullable incase of failure of parsing.   
        /// </summary>
        /// <remarks>Custom type handlers can be added here and must not throw</remarks>
        public static Dictionary<Type, object> SafeConverters = new() {
            {
                typeof(bool), new ConverterDelegate<bool?>(c => {
                    if (!bool.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(int), new ConverterDelegate<int?>(c => {
                    if (!int.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(double), new ConverterDelegate<double?>(c => {
                    {
                        if (c[^1] == '%') {
                            if (!double.TryParse(c.TrimEnd('%'), NumberStyles.Float | NumberStyles.AllowThousands, null, out var val))
                                return null;
                            return val / 100d;
                        } else {
                            if (!double.TryParse(c, NumberStyles.Float | NumberStyles.AllowThousands, null, out var val))
                                return null;
                            return val;
                        }
                    }
                })
            }, {
                typeof(long), new ConverterDelegate<long?>(c => {
                    if (!long.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(short), new ConverterDelegate<short?>(c => {
                    if (!short.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(byte), new ConverterDelegate<byte?>(c => {
                    if (!byte.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(uint), new ConverterDelegate<uint?>(c => {
                    if (!uint.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(ushort), new ConverterDelegate<ushort?>(c => {
                    if (!ushort.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(sbyte), new ConverterDelegate<sbyte?>(c => {
                    if (!sbyte.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(float), new ConverterDelegate<float?>(c => {
                    if (!float.TryParse(c, out var val))
                        return null;
                    return val;
                })
            },
            { typeof(DateTime), new ConverterDelegate<DateTime?>(ConfigParsers.Safe.DateTime) },
            { typeof(TimeSpan), new ConverterDelegate<TimeSpan?>(ConfigParsers.Safe.TimeSpan) }, {
                typeof(bool?), new ConverterDelegate<bool?>(c => {
                    if (!bool.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(int?), new ConverterDelegate<int?>(c => {
                    if (!int.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(double?), new ConverterDelegate<double?>(c => {
                        if (c[^1] == '%') {
                            if (!double.TryParse(c.TrimEnd('%'), NumberStyles.Float | NumberStyles.AllowThousands, null, out var val))
                                return null;
                            return val / 100d;
                        } else {
                            if (!double.TryParse(c, NumberStyles.Float | NumberStyles.AllowThousands, null, out var val))
                                return null;
                            return val;
                        }
                    }
                )
            }, {
                typeof(long?), new ConverterDelegate<long?>(c => {
                    if (!long.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(short?), new ConverterDelegate<short?>(c => {
                    if (!short.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(byte?), new ConverterDelegate<byte?>(c => {
                    if (!byte.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(uint?), new ConverterDelegate<uint?>(c => {
                    if (!uint.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(ushort?), new ConverterDelegate<ushort?>(c => {
                    if (!ushort.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(sbyte?), new ConverterDelegate<sbyte?>(c => {
                    if (!sbyte.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(float?), new ConverterDelegate<float?>(c => {
                    if (!float.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(decimal?), new ConverterDelegate<decimal?>(c => {
                    if (!decimal.TryParse(c, out var val))
                        return null;
                    return val;
                })
            }, {
                typeof(string), new ConverterDelegate<string>(c => c.ToString())
            },
            { typeof(DateTime?), new ConverterDelegate<DateTime?>(ConfigParsers.Safe.DateTime) },
            { typeof(TimeSpan?), new ConverterDelegate<TimeSpan?>(ConfigParsers.Safe.TimeSpan) },
        };

        public static DateTime DateTime(string value) {
            if (System.DateTime.TryParseExact(value, TimeFormat, DefaultCulture, DateTimeStyles.None, out var result))
                return result;
            if (System.DateTime.TryParseExact(value, DateTimeLongFormat, DefaultCulture, DateTimeStyles.None, out result))
                return result;
            if (System.DateTime.TryParseExact(value, DateTimeFormat, DefaultCulture, DateTimeStyles.None, out result))
                return result;
            if (System.DateTime.TryParseExact(value, DateFormat, DefaultCulture, DateTimeStyles.None, out result))
                return result;

            throw new ConfigurationException($"Unable to parse '{value}' into a DateTime using one of these formats: '{TimeFormat}' , '{DateTimeLongFormat}', '{DateTimeFormat}', '{DateFormat}'");
        }

        public static TimeSpan TimeSpan(string value) {
            if (System.TimeSpan.TryParseExact(value, TimeFormat, DefaultCulture, out var result))
                return result;
            if (System.TimeSpan.TryParseExact(value, TimeFormatShort, DefaultCulture, out result))
                return result;
            if (System.TimeSpan.TryParseExact(value, "g", DefaultCulture, out result))
                return result;
            if (System.TimeSpan.TryParse(value, DefaultCulture, out result))
                return result;

            throw new ConfigurationException($"Unable to parse '{value}' into a TimeSpan using one of these formats: '{TimeFormat}' , '{TimeFormatShort}'");
        }

        public static DateTime DateTime(ReadOnlySpan<char> value) {
            if (System.DateTime.TryParseExact(value, TimeFormat, DefaultCulture, DateTimeStyles.None, out var result))
                return result;
            if (System.DateTime.TryParseExact(value, DateTimeLongFormat, DefaultCulture, DateTimeStyles.None, out result))
                return result;
            if (System.DateTime.TryParseExact(value, DateTimeFormat, DefaultCulture, DateTimeStyles.None, out result))
                return result;
            if (System.DateTime.TryParseExact(value, DateFormat, DefaultCulture, DateTimeStyles.None, out result))
                return result;

            throw new ConfigurationException($"Unable to parse '{value.ToString()}' into a DateTime using one of these formats: '{TimeFormat}' , '{DateTimeLongFormat}', '{DateTimeFormat}', '{DateFormat}'");
        }

        public static TimeSpan TimeSpan(ReadOnlySpan<char> value) {
            if (System.TimeSpan.TryParseExact(value, TimeFormat, DefaultCulture, out var result))
                return result;
            if (System.TimeSpan.TryParseExact(value, TimeFormatShort, DefaultCulture, out result))
                return result;
            if (System.TimeSpan.TryParseExact(value, "g", DefaultCulture, out result))
                return result;
            if (System.TimeSpan.TryParse(value, DefaultCulture, out result))
                return result;

            throw new ConfigurationException($"Unable to parse '{value.ToString()}' into a TimeSpan using one of these formats: '{TimeFormat}' , '{TimeFormatShort}'");
        }

        public static class Safe {
            public static DateTime? DateTime(string value) {
                if (System.DateTime.TryParseExact(value, TimeFormat, DefaultCulture, DateTimeStyles.None, out var result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeLongFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;

                return null;
            }

            public static TimeSpan? TimeSpan(string value) {
                if (System.TimeSpan.TryParseExact(value, TimeFormat, DefaultCulture, out var result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, TimeFormatShort, DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, "g", DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParse(value, DefaultCulture, out result))
                    return result;

                return null;
            }

            public static DateTime DateTime(string value, DateTime @default) {
                if (System.DateTime.TryParseExact(value, TimeFormat, DefaultCulture, DateTimeStyles.None, out var result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeLongFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;

                return @default;
            }

            public static TimeSpan TimeSpan(string value, TimeSpan @default) {
                if (System.TimeSpan.TryParseExact(value, TimeFormat, DefaultCulture, out var result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, TimeFormatShort, DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, "g", DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParse(value, DefaultCulture, out result))
                    return result;

                return @default;
            }

            public static DateTime? DateTime(string value, DateTime? @default) {
                if (System.DateTime.TryParseExact(value, TimeFormat, DefaultCulture, DateTimeStyles.None, out var result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeLongFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;

                return @default;
            }

            public static TimeSpan? TimeSpan(string value, TimeSpan? @default) {
                if (System.TimeSpan.TryParseExact(value, TimeFormat, DefaultCulture, out var result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, TimeFormatShort, DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, "g", DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParse(value, DefaultCulture, out result))
                    return result;

                return @default;
            }


            public static DateTime? DateTime(ReadOnlySpan<char> value) {
                if (System.DateTime.TryParseExact(value, TimeFormat, DefaultCulture, DateTimeStyles.None, out var result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeLongFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;

                return null;
            }

            public static TimeSpan? TimeSpan(ReadOnlySpan<char> value) {
                if (System.TimeSpan.TryParseExact(value, TimeFormat, DefaultCulture, out var result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, TimeFormatShort, DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, "g", DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParse(value, DefaultCulture, out result))
                    return result;

                return null;
            }

            public static DateTime DateTime(ReadOnlySpan<char> value, DateTime @default) {
                if (System.DateTime.TryParseExact(value, TimeFormat, DefaultCulture, DateTimeStyles.None, out var result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeLongFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;

                return @default;
            }

            public static TimeSpan TimeSpan(ReadOnlySpan<char> value, TimeSpan @default) {
                if (System.TimeSpan.TryParseExact(value, TimeFormat, DefaultCulture, out var result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, TimeFormatShort, DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, "g", DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParse(value, DefaultCulture, out result))
                    return result;

                return @default;
            }

            public static DateTime? DateTime(ReadOnlySpan<char> value, DateTime? @default) {
                if (System.DateTime.TryParseExact(value, TimeFormat, DefaultCulture, DateTimeStyles.None, out var result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeLongFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateTimeFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;
                if (System.DateTime.TryParseExact(value, DateFormat, DefaultCulture, DateTimeStyles.None, out result))
                    return result;

                return @default;
            }

            public static TimeSpan? TimeSpan(ReadOnlySpan<char> value, TimeSpan? @default) {
                if (System.TimeSpan.TryParseExact(value, TimeFormat, DefaultCulture, out var result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, TimeFormatShort, DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParseExact(value, "g", DefaultCulture, out result))
                    return result;
                if (System.TimeSpan.TryParse(value, DefaultCulture, out result))
                    return result;

                return @default;
            }
        }
    }
}