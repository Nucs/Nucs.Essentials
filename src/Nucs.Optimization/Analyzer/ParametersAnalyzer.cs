using System.Diagnostics;
using Nucs.Optimization.Attributes;

namespace Nucs.Optimization.Analyzer;

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
        if (ParameterNames != null)
            return; //for debugging already initialized.

        var p_attr = typeof(TParams).GetCustomAttribute<ParametersAttribute>();

        foreach (MemberInfo field in typeof(TParams).GetFields().Cast<MemberInfo>().Concat(typeof(TParams).GetProperties().Cast<MemberInfo>())) {
            Type valueType = (field as FieldInfo)?.FieldType ?? (field as PropertyInfo)?.PropertyType!;
            if (field.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
                continue;

            var tc = GetPrimitiveTypeCode(valueType);
            var declaration = field.GetCustomAttributes<DimensionAttribute>().ToArray();

            DimensionAttribute space;
            switch (declaration.Length) {
                case > 1: throw new Exception("Multiple dimension space attributes are not allowed.");
                case 1:
                    //already declared
                    space = declaration[0];
                    break;
                default: {
                    //resolve
                    if (p_attr is { Inclusion: ParametersInclusion.ExplicitOnly })
                        continue;

                    space = tc switch {
                        TypeCode.Char    => new IntegerSpace<char>(char.MinValue, char.MaxValue),
                        TypeCode.SByte   => new IntegerSpace<sbyte>(sbyte.MinValue, sbyte.MaxValue),
                        TypeCode.Byte    => new IntegerSpace<byte>(byte.MinValue, byte.MaxValue),
                        TypeCode.Int16   => new IntegerSpace<short>(short.MinValue, short.MaxValue),
                        TypeCode.UInt16  => new IntegerSpace<ushort>(ushort.MinValue, ushort.MaxValue),
                        TypeCode.Int32   => new IntegerSpace<int>(int.MinValue, int.MaxValue),
                        TypeCode.UInt32  => new IntegerSpace<uint>(uint.MinValue, uint.MaxValue),
                        TypeCode.Int64   => new IntegerSpace<long>(long.MinValue, long.MaxValue),
                        TypeCode.UInt64  => new IntegerSpace<ulong>(ulong.MinValue, ulong.MaxValue),
                        TypeCode.Single  => new RealSpace<float>(float.MinValue, float.MaxValue),
                        TypeCode.Double  => new RealSpace<double>(double.MinValue, double.MaxValue),
                        TypeCode.Decimal => new RealSpace<decimal>(decimal.MinValue, decimal.MaxValue),
                        TypeCode.Boolean => new CategoricalSpace<bool>(new bool[] { true, false }),
                        TypeCode.String => valueType.IsEnum
                            ? (DimensionAttribute) typeof(CategoricalSpace<>).MakeGenericType(valueType).GetConstructor(new[] { valueType.MakeArrayType() }).Invoke(new object[] { Enum.GetValues(valueType) })
                            : throw new ArgumentException($"{field.Name} has to be decorated with [CategoricalSpace<string>(...)]"),

                        _ => throw new ArgumentOutOfRangeException()
                    };
                    break;
                }
            }

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
                TypeCode.Boolean => CreateCategorical<bool>(),
                TypeCode.String => valueType.IsEnum
                    ? CreateEnum(valueType)
                    : CreateCategorical<string>(),

                _ => throw new ArgumentOutOfRangeException()
            };

            Parameters.Add(param.Name, param);

            ParameterType CreateNumerical<T>() where T : INumber<T>, IMinMaxValue<T> {
                if (space is CategoricalSpace) {
                    var lhs = Expression.Variable(typeof(TParams), "lhs");
                    var rhs = Expression.Variable(typeof(T), "rhs");
                    var accessor = Expression.Lambda<CategoricalParameterType<T>.AssignDelegate<TParams>>(
                        Expression.Assign(Expression.PropertyOrField(lhs, field.Name), rhs), lhs, rhs).Compile();
                    //categorical
                    return new CategoricalParameterType<T>(field.Name, tc, accessor, space);
                } else {
                    var lhs = Expression.Variable(typeof(TParams), "lhs");
                    var rhs = Expression.Variable(typeof(T), "rhs");
                    var accessor = Expression.Lambda<NumericalParameterType<T>.AssignDelegate<TParams>>(
                        Expression.Assign(Expression.PropertyOrField(lhs, field.Name), rhs), lhs, rhs).Compile();
                    var param = new NumericalParameterType<T>(field.Name, tc, accessor, space);
                    return param;
                }
            }

            ParameterType CreateCategorical<T>() {
                var lhs = Expression.Variable(typeof(TParams), "lhs");
                var rhs = Expression.Variable(typeof(T), "rhs");
                var accessor = Expression.Lambda<CategoricalParameterType<T>.AssignDelegate<TParams>>(
                    Expression.Assign(Expression.PropertyOrField(lhs, field.Name), rhs), lhs, rhs).Compile();
                return new CategoricalParameterType<T>(field.Name, tc, accessor, space);
            }

            ParameterType CreateEnum(Type enumType) {
                Expression<Func<Type, string, Enum>> asEnum = (type, s) => (Enum) Enums.Parse(type, s, true);
                Expression<Func<Type, Enum, string>> asString = (type, s) => Enums.AsString(type, s);

                var lhs = Expression.Variable(typeof(TParams), "lhs");
                var rhs = Expression.Variable(typeof(string), "rhs");
                //var asStringInvocation = Expression.Invoke(asString, Expression.Constant(enumType, typeof(Type)), Expression.Convert(rhs, typeof(Enum)));
                var asEnumInvocation = Expression.Convert(Expression.Invoke(asEnum, Expression.Constant(enumType, typeof(Type)), rhs), enumType);
                var accessor = Expression.Lambda<CategoricalParameterType<string>.AssignDelegate<TParams>>(
                    Expression.Assign(Expression.PropertyOrField(lhs, field.Name), asEnumInvocation), lhs, rhs).Compile();

                return (ParameterType) typeof(CategoricalParameterType<>)
                                      .MakeGenericType(valueType)
                                      .GetConstructor(new[] { typeof(string), typeof(TypeCode), typeof(CategoricalParameterType<string>.AssignDelegate<TParams>), typeof(DimensionAttribute) })
                                      .Invoke(new object[] { field.Name, tc, accessor, space });
            }
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
    public static void Apply(TParams parameters, List<Tuple<string, object>> values, bool sortFirst = false) {
        if (sortFirst)
            values.Sort((l, r) => Array.IndexOf(ParameterNames, l.Item1).CompareTo(Array.IndexOf(ParameterNames, r.Item1)));

        for (var i = 0; i < values.Count; i++) {
            var (name, value) = values[i];
            var parameter = ParameterValues[i];

            //test sequential order
            Debug.Assert(object.ReferenceEquals(Parameters[name], parameter));

            parameter.Assign(parameters, value);
        }
    }


    /// <summary>
    ///     Applies sequential <see cref="values"/> to <see cref="TParams"/> based on <see cref="Parameters"/> order.
    /// </summary>
    public static void Apply(TParams parameters, List<(string Name, object Value)> values, bool sortFirst = false) {
        if (sortFirst)
            values.Sort((l, r) => Array.IndexOf(ParameterNames, l.Item1).CompareTo(Array.IndexOf(ParameterNames, r.Item1)));

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