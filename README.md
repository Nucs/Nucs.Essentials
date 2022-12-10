# <img src="https://i.imgur.com/BOExs52.png" width="25" style="margin: 5px 0px 0px 10px"/> Nucs.Essentials
[![Nuget downloads](https://img.shields.io/nuget/vpre/Nucs.Essentials.svg)](https://www.nuget.org/packages/Nucs.Essentials/)
[![NuGet](https://img.shields.io/nuget/dt/Nucs.Essentials.svg)](https://github.com/Nucs/Nucs.Essentials)
[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Nucs/Essentials/blob/master/LICENSE)

If you had a bunch of good generic classes, would you not place them in a nuget package?<br/>
This library contains essential classes I use in production.<br/>
Cloning and exploring this repository is the recommended way of learning how to use it. It is not meant for the juniors.

### Installation
```sh
PM> Install-Package Nucs.Essentials
```


# <img src="https://i.imgur.com/BOExs52.png" width="25" style="margin: 5px 0px 0px 10px"/> Nucs.Optimization
[![Nuget downloads](https://img.shields.io/nuget/vpre/Nucs.Optimization.svg)](https://www.nuget.org/packages/Nucs.Optimization/)
[![NuGet](https://img.shields.io/nuget/dt/Nucs.Optimization.svg)](https://github.com/Nucs/Nucs.Optimization)
[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Nucs/Essentials/blob/master/LICENSE)

A .NET binding to skopt (scikit-optimize) - a optimization library with support to dynamic search spaces through generic binding.<br/>

*For installation, [continue here](https://github.com/Nucs/Nucs.Essentials/tree/main/src/Nucs.Optimization)*


```C#
[Maximize] //or [Minimize]
double ScoreFunction(Parameters parameters) {
    var res = (parameters.Seed * parameters.NumericalCategories * (parameters.UseMethod ? 1 : -1)) / 1000000;
    Console.WriteLine($"Score: {res}, Parameters: {parameters}");
    return res;
}


var opt = new PyBasyesianOptimization<Parameters>(ScoreFunction);
var opt2 = new PyForestOptimization<Parameters>(ScoreFunction);
var opt3 = new PyRandomOptimization<Parameters>(ScoreFunction);
(double Score, Parameters Parameters) = opt.Search(n_calls: 100, 10, verbose: false);
(double Score, Parameters Parameters) = opt2.Search(n_calls: 100, 10, verbose: false);
(double Score, Parameters Parameters) = opt3.Search(n_calls: 100, verbose: false);
```

```C#
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

public enum SomeEnum {
    A,
    B,
    C
}

```
