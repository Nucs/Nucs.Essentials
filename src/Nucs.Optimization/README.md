
# <img src="https://i.imgur.com/BOExs52.png" width="25" style="margin: 5px 0px 0px 10px"/> Nucs.Optimization
[![Nuget downloads](https://img.shields.io/nuget/vpre/Nucs.Optimization.svg)](https://www.nuget.org/packages/Nucs.Essentials/)
[![NuGet](https://img.shields.io/nuget/dt/Nucs.Optimization.svg)](https://github.com/Nucs/Nucs.Essentials)
[![GitHub license](https://img.shields.io/github/license/mashape/apistatus.svg)](https://github.com/Nucs/Essentials/blob/master/LICENSE)

A .NET binding using [pythonnet](https://github.com/pythonnet/pythonnet) for [skopt (scikit-optimize)](https://scikit-optimize.github.io/) - an optimization library with support to dynamic search spaces through generic binding.<br/>

Available Algorithms:
- [x] [Random Search](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Optimization/PyRandomOptimization.cs)
- [x] [Bayesian Optimization](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Optimization/PyBayesianOptimization.cs)
- [x] [Random Forest Optimization](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Optimization/PyForestOptimization.cs)
- [x] [Gradient Boosting Regression Trees (Gbrt)](https://github.com/Nucs/Nucs.Essentials/blob/main/src/Nucs.Optimization/PyGbrtOptimization.cs)

*Source code can be, [found here](https://github.com/Nucs/Nucs.Essentials/tree/main/src/Nucs.Optimization)*


### Installation

* Python 3.8+
    ```sh
    numpy>=1.23.5
    pythonnet>=3.0.1
    scikit-learn>=1.2.0
    scikit-optimize>=0.9.0
    scipy>=1.9.3
  
    > pip install numpy pythonnet scikit-learn scikit-optimize scipy
    ```
* .NET 7.0
    ```sh
    PM> Install-Package Nucs.Optimization
    ```

### Getting Started


Declare a parameters class/record for the optimization search space.</br>
Annotate it with IntegerSpace / RealSpace / CategoricalSpace attributes. </br>
Non-annotated parameters will be implicitly included by default.

```C#

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

```

```C#
//setup python runtime
Runtime.PythonDLL = Environment.ExpandEnvironmentVariables("%APPDATA%\\..\\Local\\Programs\\Python\\Python38\\python38.dll");
PythonEngine.Initialize();
PythonEngine.BeginAllowThreads();
using var py = Py.GIL(); //no GIL is being taken inside. has to be taken outside.

//declare a function to optimize
[Maximize] //or [Minimize]
double ScoreFunction(Parameters parameters) {
    return (parameters.Seed * parameters.NumericalCategories * (parameters.UseMethod ? 1 : -1) * Math.Sin(0.05+parameters.FloatSeed)) / 1000000;
}

//construct an optimizer
var opt = new PyBayesianOptimization<Parameters>(ScoreFunction);
var opt2 = new PyForestOptimization<Parameters>(ScoreFunction);
var opt3 = new PyRandomOptimization<Parameters>(ScoreFunction);
var opt4 = new PyGbrtOptimization<Parameters>(ScoreFunction);

//(optional) prepare callbacks
var callbacks = new PyOptCallback[] { new IterationCallback<Parameters>(maximize: true, (iteration, parameters, score) => {
    Console.WriteLine($"[{iteration}] Score: {score}, Parameters: {parameters}");
})};

//run optimizer of choice (Search, SearchTop, SearchAll)
double Score;
Parameters Parameters;
(Score, Parameters) = opt.Search(n_calls: 100, n_random_starts: 10, verbose: false, callbacks: callbacks);
(Score, Parameters) = opt2.Search(n_calls: 100, n_random_starts: 10, verbose: false, callbacks: callbacks);
(Score, Parameters) = opt3.Search(n_calls: 100, verbose: false, callbacks: callbacks);
(Score, Parameters) = opt4.Search(n_calls: 100, n_random_starts: 10, verbose: false, callbacks: callbacks);
```
