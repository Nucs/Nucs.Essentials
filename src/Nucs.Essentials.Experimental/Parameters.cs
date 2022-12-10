using System.Runtime.Serialization;
using Nucs.Optimization.Attributes;

public enum SomeEnum {
    A,
    B,
    C
}

public record Parameters {
    [Range<int>(0, int.MaxValue)]
    public int Seed; //range of 0 to int.MaxValue

    [Range<double>(0, 1)]
    public double FloatSeed; //range of 0 to int.MaxValue

    [Values("A", "B", "C")]
    public string Categories; //one of "A", "B", "C"

    [Values(1f, 2f, 3f)]
    public float NumericalCategories { get; set; } //one of 1f, 2f, 3f

    public bool UseMethod; //true or false

    public SomeEnum AnEnum; //one of the enum values ("A", "B", "C")

    [Values(SomeEnum.A, SomeEnum.B)]
    public SomeEnum AnEnumWithValues; //one of the enum values ("A", "B")

    [IgnoreDataMember]
    public bool Ignored; //will be ignored entirely
    
    [IgnoreDataMember]
    public bool IgnoredProperty { get; set; } //will be ignored entirely

    public override string ToString() {
        return $"{nameof(Seed)}: {Seed}, {nameof(FloatSeed)}: {FloatSeed.ToString()}, {nameof(Categories)}: {Categories}, {nameof(UseMethod)}: {UseMethod}, {nameof(AnEnum)}: {AnEnum}, {nameof(AnEnumWithValues)}: {AnEnumWithValues}, {nameof(NumericalCategories)}: {NumericalCategories}";
    }
}