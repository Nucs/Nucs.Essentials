```C#
using Nucs.Optimization;
using Nucs.Optimization.Attributes;
using Python.Runtime;
using System.Runtime.Serialization;
using Nucs.Optimization.Attributes;

//define parameters in a record/class
public record Parameters {
    [Range<int>(0, int.MaxValue)]
    public int Seed; //range of 0 to int.MaxValue

    [Range<double>(0, double.MaxValue)]
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
        return $"{nameof(Seed)}: {Seed}, {nameof(Categories)}: {Categories}, {nameof(UseMethod)}: {UseMethod}, {nameof(AnEnum)}: {AnEnum}, {nameof(AnEnumWithValues)}: {AnEnumWithValues}, {nameof(Ignored)}: {Ignored}, {nameof(NumericalCategories)}: {NumericalCategories}";
    }
}

public enum SomeEnum {
    A,
    B,
    C
}
```

Define Score function, Configure python runtime and run the optimization:

```C#

//blackbox scoring function
[Maximize] //or Minimize which is default.
static double ScoreFunction(Parameters parameters) {
    //calculate score
    var res = (parameters.Seed * parameters.NumericalCategories * (parameters.UseMethod ? 1 : -1)) / 1000000;
    Console.WriteLine($"Score: {res}, Parameters: {parameters}"); //will report the score and the parameters after each run
    return res;
}

//configure python runtime
Runtime.PythonDLL = Environment.ExpandEnvironmentVariables("%APPDATA%\\..\\Local\\Programs\\Python\\Python38\\python38.dll");
PythonEngine.Initialize();
PythonEngine.BeginAllowThreads();
using var py = Py.GIL();

//run optimization
var opt = new PyBasyesianOptimization<Parameters>(ScoreFunction);
(double Score, Parameters Parameters) = opt.Search(n_calls: 15, n_random_starts: 10, verbose: false);
Console.WriteLine($"Best Score: {Score} for {Parameters}");
```

Output:

```
Score: -4017.2786, Parameters: Seed: 1339092842, Categories: C, UseMethod: False, AnEnum: B, AnEnumWithValues: B, Ignored: False, NumericalCategories: 3
Score: -3488.238, Parameters: Seed: 1744119064, Categories: B, UseMethod: False, AnEnum: B, AnEnumWithValues: A, Ignored: False, NumericalCategories: 2
Score: 1581.5854, Parameters: Seed: 790792685, Categories: B, UseMethod: True, AnEnum: B, AnEnumWithValues: B, Ignored: False, NumericalCategories: 2
Score: 3353.1504, Parameters: Seed: 1117716876, Categories: B, UseMethod: True, AnEnum: A, AnEnumWithValues: B, Ignored: False, NumericalCategories: 3
Score: -682.3045, Parameters: Seed: 227434855, Categories: B, UseMethod: False, AnEnum: B, AnEnumWithValues: B, Ignored: False, NumericalCategories: 3
Score: -696.08746, Parameters: Seed: 696087514, Categories: A, UseMethod: False, AnEnum: A, AnEnumWithValues: B, Ignored: False, NumericalCategories: 1
Score: 2633.0874, Parameters: Seed: 1316543750, Categories: C, UseMethod: True, AnEnum: A, AnEnumWithValues: A, Ignored: False, NumericalCategories: 2
Score: 769.126, Parameters: Seed: 769125922, Categories: B, UseMethod: True, AnEnum: A, AnEnumWithValues: B, Ignored: False, NumericalCategories: 1
Score: 4118.656, Parameters: Seed: 2059327873, Categories: A, UseMethod: True, AnEnum: B, AnEnumWithValues: A, Ignored: False, NumericalCategories: 2
Score: 2038.8102, Parameters: Seed: 1019405123, Categories: B, UseMethod: True, AnEnum: B, AnEnumWithValues: B, Ignored: False, NumericalCategories: 2
Score: 6442.451, Parameters: Seed: 2147483647, Categories: C, UseMethod: True, AnEnum: C, AnEnumWithValues: B, Ignored: False, NumericalCategories: 3
Score: 6442.451, Parameters: Seed: 2147483647, Categories: A, UseMethod: True, AnEnum: A, AnEnumWithValues: A, Ignored: False, NumericalCategories: 3
Score: 6442.451, Parameters: Seed: 2147483647, Categories: A, UseMethod: True, AnEnum: C, AnEnumWithValues: A, Ignored: False, NumericalCategories: 3
Score: 6442.451, Parameters: Seed: 2147483647, Categories: A, UseMethod: True, AnEnum: A, AnEnumWithValues: B, Ignored: False, NumericalCategories: 3
Score: 6442.451, Parameters: Seed: 2147483647, Categories: C, UseMethod: True, AnEnum: A, AnEnumWithValues: A, Ignored: False, NumericalCategories: 3
Best Score: 6442.451171875 for Seed: 2147483647, Categories: C, UseMethod: True, AnEnum: C, AnEnumWithValues: B, Ignored: False, NumericalCategories: 3

```

Installation:

* Python 3.8 due to the fact that pythonnet doesn't support python 3.9 and beyond yet.
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
