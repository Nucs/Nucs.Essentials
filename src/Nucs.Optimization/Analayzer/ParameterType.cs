namespace Nucs.Optimization.Analayzer; 

public abstract class ParameterType {
    public readonly string Name;
    public readonly TypeCode Type;

    public bool IsFloating;
    public bool IsNumerical;
    public abstract Type ValueType { get; }

    public abstract void Assign<TParams>(TParams parameters, object value);

    protected ParameterType(string name, TypeCode type) {
        Name = name;
        Type = type;
    }
}