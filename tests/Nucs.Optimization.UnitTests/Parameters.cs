using System;
using System.Runtime.Serialization;
using Nucs.Optimization.Attributes;

public enum SomeEnum {
    A,
    B,
    C
}

public record Parameters {
    [IntegerSpace<int>(0, int.MaxValue)]
    public int Seed; //range of 0 to int.MaxValue

    [RealSpace<double>(0, Math.PI)]
    public double FloatSeed; //range of 0 to int.MaxValue

    [CategoricalSpace<string>("A", "B", "C")]
    public string Categories; //one of "A", "B", "C"

    [CategoricalSpace<float>(1f, 2f, 3f)]
    public float NumericalCategories { get; set; } //one of 1f, 2f, 3f

    public bool UseMethod; //true or false

    public SomeEnum AnEnum; //one of the enum values ("A", "B", "C")

    [CategoricalSpace<char>('a', 'b', 'c')]
    public char Letter; //one of the enum values ('a', 'b', 'c')

    [CategoricalSpace<SomeEnum>(SomeEnum.A, SomeEnum.B)]
    public SomeEnum AnEnumWithValues; //one of the enum values ("A", "B")

    [IgnoreDataMember]
    public bool Ignored; //will be ignored entirely

    [IgnoreDataMember]
    public bool IgnoredProperty { get; set; } //will be ignored entirely

    public override string ToString() {
        return $"{nameof(Seed)}: {Seed}, {nameof(FloatSeed)}: {FloatSeed.ToString()}, {nameof(UseMethod)}: {UseMethod}, {nameof(NumericalCategories)}: {NumericalCategories}";
    }
}