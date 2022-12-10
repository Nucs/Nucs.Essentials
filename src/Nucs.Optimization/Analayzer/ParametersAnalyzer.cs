using System.Diagnostics;
using Nucs.Optimization.Attributes;
using Python.Runtime;
using RangeAttribute = Nucs.Optimization.Attributes.RangeAttribute;

namespace Nucs.Optimization.Analayzer;

public static class ParametersAnalyzer<TParams> where TParams : class, new() {
    public static readonly SortedDictionary<string, ParameterType> Parameters = new SortedDictionary<string, ParameterType>();
    public static ParameterType[] ParameterValues;
    public static string[] ParameterNames;
    public static int ParametersCount => Parameters.Count;

    #if DEBUG
    public static void Initialize() {
        #else
    public static void Initialize() { }
    static ParametersAnalyzer() {
        #endif

        foreach (MemberInfo field in typeof(TParams).GetFields().Cast<MemberInfo>().Concat(typeof(TParams).GetProperties().Cast<MemberInfo>())) {
            Type valueType = (field as FieldInfo)?.FieldType ?? (field as PropertyInfo)?.PropertyType!;
            if (field.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
                continue;

            var tc = GetPrimitiveTypeCode(valueType);
            var valuesAttr = field.GetCustomAttribute<ValuesAttribute>();
            var rangeAttr = field.GetCustomAttribute<RangeAttribute>();

            ParameterType param = tc switch {
                TypeCode.Char    => CreateNumerical<char>(),
                TypeCode.SByte   => CreateNumerical<sbyte>(),
                TypeCode.Byte    => CreateNumerical<byte>(),
                TypeCode.Int16   => CreateNumerical<short>(),
                TypeCode.UInt16  => CreateNumerical<ushort>(),
                TypeCode.Int32   => CreateNumerical<int>(),
                TypeCode.UInt32  => CreateNumerical<uint>(),
                TypeCode.Int64   => CreateNumerical<long>(),
                TypeCode.UInt64  => CreateNumerical<ulong>(),
                TypeCode.Single  => CreateNumerical<float>(),
                TypeCode.Double  => CreateNumerical<double>(),
                TypeCode.Decimal => CreateNumerical<decimal>(),
                TypeCode.Boolean => CreateCategorical<bool>(new bool[] { true, false }),
                TypeCode.String => valueType.IsEnum
                    ? CreateEnum(valueType, valuesAttr?.Values.Select(v => v as string ?? v.ToString()).ToArray() ?? Enums.GetNames(valueType).ToArray())
                    : CreateCategorical<string>(valuesAttr?.Values.Select(v => v as string ?? v.ToString()).ToArray() ?? throw new ConstraintException($"No values specified for {field.Name} via {nameof(ValuesAttribute)}")),

                _ => throw new ArgumentOutOfRangeException()
            };

            ParameterType CreateNumerical<T>() where T : INumber<T>, IMinMaxValue<T> {
                if (valuesAttr != null) {
                    var lhs = Expression.Variable(typeof(TParams), "lhs");
                    var rhs = Expression.Variable(typeof(T), "rhs");
                    var accessor = Expression.Lambda<CategoricalParameterType<T>.AssignDelegate<TParams>>(
                        Expression.Assign(Expression.PropertyOrField(lhs, field.Name), rhs), lhs, rhs).Compile();
                    //categorical
                    return new CategoricalParameterType<T>(field.Name, tc, accessor, valuesAttr.Values.Select(v => (T) Convert.ChangeType(v, typeof(T))).ToArray());
                } else {
                    var lhs = Expression.Variable(typeof(TParams), "lhs");
                    var rhs = Expression.Variable(typeof(T), "rhs");
                    var accessor = Expression.Lambda<NumericalParameterType<T>.AssignDelegate<TParams>>(
                        Expression.Assign(Expression.PropertyOrField(lhs, field.Name), rhs), lhs, rhs).Compile();
                    var param = new NumericalParameterType<T>(field.Name, tc, accessor, rangeAttr ?? throw new ConstraintException($"No range specified for {field} via {nameof(ValuesAttribute)}"));
                    return param;
                }
            }

            ParameterType CreateCategorical<T>(T[] values) {
                var lhs = Expression.Variable(typeof(TParams), "lhs");
                var rhs = Expression.Variable(typeof(T), "rhs");
                var accessor = Expression.Lambda<CategoricalParameterType<T>.AssignDelegate<TParams>>(
                    Expression.Assign(Expression.PropertyOrField(lhs, field.Name), rhs), lhs, rhs).Compile();
                return new CategoricalParameterType<T>(field.Name, tc, accessor, values);
            }

            ParameterType CreateEnum(Type enumType, string[] values) {
                Expression<Func<Type, string, Enum>> asEnum = (type, s) => (Enum) Enums.Parse(type, s, true);
                Expression<Func<Type, Enum, string>> asString = (type, s) => Enums.AsString(type, s);

                var lhs = Expression.Variable(typeof(TParams), "lhs");
                var rhs = Expression.Variable(typeof(string), "rhs");
                //var asStringInvocation = Expression.Invoke(asString, Expression.Constant(enumType, typeof(Type)), Expression.Convert(rhs, typeof(Enum)));
                var asEnumInvocation = Expression.Convert(Expression.Invoke(asEnum, Expression.Constant(enumType, typeof(Type)), rhs), enumType);
                var accessor = Expression.Lambda<CategoricalParameterType<string>.AssignDelegate<TParams>>(
                    Expression.Assign(Expression.PropertyOrField(lhs, field.Name), asEnumInvocation), lhs, rhs).Compile();

                return new CategoricalParameterType<string>(field.Name, tc, accessor, values);
            }

            Parameters.Add(param.Name, param);
        }

        ParameterNames = Parameters.Select(s => s.Key).ToArray();
        ParameterValues = Parameters.Select(s => s.Value).ToArray();
    }

    private static TypeCode GetPrimitiveTypeCode(Type type) {
        if (type.IsEnum) {
            return TypeCode.String;
        }

        var tc = Type.GetTypeCode(type);
        switch (tc) {
            case TypeCode.Empty:
            case TypeCode.Object:
            case TypeCode.DBNull:
            case TypeCode.DateTime:
                throw new NotSupportedException($"Given type is not supported: {type}");
            default: return tc;
        }
    }

    /// <summary>
    ///     Applies sequential <see cref="values"/> to <see cref="TParams"/> based on <see cref="Parameters"/> order.
    /// </summary>
    public static void Apply(TParams parameters, List<Tuple<string, object>> values) {
        for (var i = 0; i < values.Count; i++) {
            var (name, value) = values[i];
            var parameter = ParameterValues[i];

            //test sequential order
            Debug.Assert(object.ReferenceEquals(Parameters[name], parameter));

            parameter.Assign(parameters, value);
        }
    }

    /// <summary>
    ///     Populates sequential <see cref="values"/> to <see cref="TParams"/> based on <see cref="Parameters"/> order.
    /// </summary>
    public static TParams Populate(List<Tuple<string, object>> values) {
        var parameters = new TParams();
        Apply(parameters, values);
        return parameters;
    }
}