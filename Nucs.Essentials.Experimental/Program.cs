using Nucs.Optimization;
using Nucs.Optimization.Attributes;
using Python.Runtime;

[Maximize]
static double ScoreFunction(Parameters parameters) {
    var res = (parameters.Seed * parameters.NumericalCategories * (parameters.UseMethod ? 1 : -1)) / 1000000;
    Console.WriteLine($"Score: {res}, Parameters: {parameters}");
    return res;
}

Runtime.PythonDLL = Environment.ExpandEnvironmentVariables("%APPDATA%\\..\\Local\\Programs\\Python\\Python38\\python38.dll");
PythonEngine.Initialize();
PythonEngine.BeginAllowThreads();
using var py = Py.GIL();

var opt = new PyBasyesianOptimization<Parameters>(ScoreFunction);
(double Score, Parameters Parameters) = opt.Search(n_calls: 15, n_random_starts: 10, verbose: false);
Console.WriteLine($"Best Score: {Score} for {Parameters}");