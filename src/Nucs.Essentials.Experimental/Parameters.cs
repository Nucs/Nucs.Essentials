using System.Runtime.Serialization;
using Nucs.Optimization.Attributes;

[Parameters(Inclusion = ParametersInclusion.ImplicitAndExplicit)] //include all annotated and non-annotated
public record Parameters {
    [IntegerSpace<int>(1, int.MaxValue, Prior = Prior.LogUniform, Base = 2, Transform = NumericalTransform.Normalize)]
    public int Seed; //range of 0 to int.MaxValue (including)

    [RealSpace<double>(0, Math.PI)]
    public double FloatSeed; //range of 0 to int.MaxValue (including)

    [CategoricalSpace<float>(1f, 2f, 3f)]
    public float NumericalCategories { get; set; } //one of 1f, 2f, 3f

    [CategoricalSpace<double>(1d, 10d, 100d, 1000d)]
    public double LogNumericalCategories { get; set; } //one of 1d, 10d, 100d, 1000d

    [CategoricalSpace<string>("A", "B", "C", Transform = CategoricalTransform.Identity)]
    public string Categories; //one of "A", "B", "C"

    [CategoricalSpace<bool>] //optional, will be included implicitly
    public bool UseMethod; //true or false

    [CategoricalSpace<SomeEnum>(SomeEnum.A, SomeEnum.B, SomeEnum.C)]
    public SomeEnum AnEnum; //one of the enum values ("A", "B", "C")

    /// string will be parsed to SomeEnum. Prior provides the priority of each possible value. 'B' will have 80% priority of being selected.
    [CategoricalSpace<SomeEnum>("A", "B", Prior = new double[] {0.2, 0.8})] 
    public SomeEnum AnEnumWithValues; //one of the enum values ("A", "B")

    public SomeEnum AllValuesOfEnum; //one of any of the values of the enum
        
    [IgnoreDataMember]
    public bool Ignored; //will be ignored entirely
}

public enum SomeEnum { A, B, C }