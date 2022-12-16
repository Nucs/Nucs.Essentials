using Nucs;
using Nucs.Optimization;
using Nucs.Optimization.Attributes;
using Python.Runtime;

Runtime.PythonDLL = Environment.ExpandEnvironmentVariables("%APPDATA%\\..\\Local\\Programs\\Python\\Python38\\python38.dll");
PythonEngine.Initialize();
PythonEngine.BeginAllowThreads();
using var py = Py.GIL();


[Maximize]
static double ScoreFunction(Parameters parameters) {
    var res = (parameters.Seed * parameters.NumericalCategories * (parameters.UseMethod ? 1 : -1) * Math.Sin(0.05+parameters.FloatSeed)) / 1000000;
    Console.WriteLine($"Score: {res}, Parameters: {parameters}");
    return res;
}

var opt = new PyBasyesianOptimization<Parameters>(ScoreFunction);
//(double Score, Parameters Parameters) = opt.Search(n_calls: 100, 10, verbose: false);

var optimal = new Parameters() {
    Seed = int.MaxValue,
    NumericalCategories = 3,
    FloatSeed = Math.PI/2 - 0.05d,
    UseMethod = true,
};

Console.WriteLine($"Optimal: {ScoreFunction(optimal)}, Parameters: {optimal}");

var t = opt.SearchTop(10, n_calls: 200, 100, verbose: false);

foreach ((double Score, Parameters Parameters) in t) {
    Console.WriteLine($"Best Score: {Score} for {Parameters}");
}

;
