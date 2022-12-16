using System.Runtime.Serialization;
using Nucs.Optimization.Attributes;

public enum SomeEnum {
    A,
    B,
    C
}

[Parameters(Inclusion = ParametersInclusion.ImplicitAndExplicit)]
public record Parameters {
    [IntegerSpace<int>(0, int.MaxValue)]
    public int Seed; //range of 0 to int.MaxValue

    [RealSpace<double>(0, Math.PI)]
    public double FloatSeed; //range of 0 to int.MaxValue

    [CategoricalSpace<float>(1f, 2f, 3f)]
    public float NumericalCategories { get; set; } //one of 1f, 2f, 3f

    [CategoricalSpace<double>(1d, 10d, 100d, 1000d)]
    public double LogNumericalCategories { get; set; } //one of 1f, 2f, 3f

    [CategoricalSpace<string>("A", "B", "C", Transform = CategoricalTransform.Identity)]
    public string Categories; //one of "A", "B", "C"

    //[CategoricalSpace<bool>] //optional, will be included
    public bool UseMethod; //true or false

    [CategoricalSpace<SomeEnum>(SomeEnum.A, SomeEnum.B, SomeEnum.C)]
    public SomeEnum AnEnum; //one of the enum values ("A", "B", "C")

    [CategoricalSpace<SomeEnum>("A", "B")] //will be parsed to enum
    public SomeEnum AnEnumWithValues; //one of the enum values ("A", "B")

    public SomeEnum AnEnumWithValues2;

    public override string ToString() {
        return $"{nameof(Seed)}: {Seed}, {nameof(FloatSeed)}: {FloatSeed}, {nameof(Categories)}: {Categories}, {nameof(UseMethod)}: {UseMethod}, {nameof(AnEnum)}: {AnEnum}, {nameof(AnEnumWithValues)}: {AnEnumWithValues}, {nameof(AnEnumWithValues2)}: {AnEnumWithValues2}, {nameof(NumericalCategories)}: {NumericalCategories}, {nameof(LogNumericalCategories)}: {LogNumericalCategories}";
    }
}