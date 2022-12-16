using Nucs.Optimization.Attributes;

namespace Nucs.Optimization.Analayzer;

public abstract class ParameterType {
    public readonly string Name;
    public readonly TypeCode Type;
    public virtual DimensionAttribute Space { get; set; }

    public bool IsFloating { get; private set; }
    public bool IsNumerical { get; private set; }
    public abstract Type ValueType { get; }

    public abstract void Assign<TParams>(TParams parameters, object value);

    protected ParameterType(string name, TypeCode type, DimensionAttribute space) {
        Space = space;
        Name = name;
        Type = type;

        IsNumerical = type switch {
            TypeCode.Char     => true,
            TypeCode.SByte    => true,
            TypeCode.Byte     => true,
            TypeCode.Int16    => true,
            TypeCode.UInt16   => true,
            TypeCode.Int32    => true,
            TypeCode.UInt32   => true,
            TypeCode.Int64    => true,
            TypeCode.UInt64   => true,
            TypeCode.Decimal  => true,
            TypeCode.Double   => true,
            TypeCode.Single   => true,
            TypeCode.Boolean  => false,
            TypeCode.DateTime => false,
            TypeCode.String   => false,
            _                 => false
        };

        IsFloating = type switch {
            TypeCode.Char     => false,
            TypeCode.SByte    => false,
            TypeCode.Byte     => false,
            TypeCode.Int16    => false,
            TypeCode.UInt16   => false,
            TypeCode.Int32    => false,
            TypeCode.UInt32   => false,
            TypeCode.Int64    => false,
            TypeCode.UInt64   => false,
            TypeCode.Decimal  => true,
            TypeCode.Double   => true,
            TypeCode.Single   => true,
            TypeCode.Boolean  => false,
            TypeCode.DateTime => false,
            TypeCode.String   => false,
            _                 => false
        };
    }
}