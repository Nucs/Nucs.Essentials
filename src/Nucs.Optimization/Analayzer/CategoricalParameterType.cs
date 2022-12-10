namespace Nucs.Optimization.Analayzer;

public abstract class CategoricalParameterType : ParameterType {
    public int UpperThreshold;
    public int LowerThreshold;
    public int Difference; //aka sigma

    public abstract object[] ObjectValues { get; }

    public CategoricalParameterType(string name, TypeCode type) : base(name, type) { }
}

public class CategoricalParameterType<T> : CategoricalParameterType {
    public static bool IsEnum = typeof(T).IsEnum;
    internal delegate void AssignDelegate<in TParams>(TParams parameters, T value);
    public readonly T[] Values;
    public override object[] ObjectValues => Values.Select(v => (object) v).ToArray();

    internal readonly Delegate AssignPointer;

    public void Assign<TParams>(TParams parameters, T value) {
        ((CategoricalParameterType<T>.AssignDelegate<TParams>) AssignPointer)(parameters, value);
    }

    public override void Assign<TParams>(TParams parameters, object value) {
        if (value is T tval)
            ((CategoricalParameterType<T>.AssignDelegate<TParams>) AssignPointer)(parameters, tval);
        else
            ((CategoricalParameterType<T>.AssignDelegate<TParams>) AssignPointer)(parameters, (T) Convert.ChangeType(value, typeof(T)));
    }


    public CategoricalParameterType(string name, TypeCode type, Delegate assignPointer, T[] values) : base(name, type) {
        AssignPointer = assignPointer;
        Values = values;
        LowerThreshold = 0;
        UpperThreshold = Values.Length - 1;
        Difference = Values.Length - 1;
        IsNumerical = type switch {
            TypeCode.UInt16 => true,
            TypeCode.UInt32 => true,
            TypeCode.UInt64 => true,
            TypeCode.Int16  => true,
            TypeCode.Int32  => true,
            TypeCode.Int64  => true,
            TypeCode.Single => true,
            TypeCode.Double => true,
            _               => false
        };
    }

    public override Type ValueType => typeof(T);
}