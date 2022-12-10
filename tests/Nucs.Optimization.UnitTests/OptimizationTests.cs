using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;
using DotNext.Threading.Tasks;
using FluentAssertions;
using Nucs.Optimization;
using Nucs.Optimization.Analayzer;
using Nucs.Optimization.Attributes;
using Nucs.Threading;
using Python.Runtime;
using Xunit;
using Xunit.Abstractions;
using AsyncCountdownEvent = Nucs.Threading.AsyncCountdownEvent;

namespace Nucs.Essentials.UnitTests;

public class OptimizationTests : PythonTest {
    private readonly ITestOutputHelper Console;
    private Parameters _optimalParameters;
    private double _optimal;

    [Maximize]
    double ScoreFunction(Parameters parameters) {
        /** Math.Min(0.5, Math.Max(0.00000001, parameters.FloatSeed - 0.75)*/
        var res = (parameters.Seed * parameters.NumericalCategories * (parameters.UseMethod ? 1 : -1) * Math.Sin(0.05 + parameters.FloatSeed)) / 1000000;
        Console.WriteLine($"Score: {res}, Parameters: {parameters}");
        return res;
    }

    public OptimizationTests(ITestOutputHelper console) : base() {
        Console = console;
        
        _optimalParameters = new Parameters() {
            Seed = int.MaxValue,
            NumericalCategories = 3,
            FloatSeed = Math.PI/2 - 0.05d,
            UseMethod = true,
        };

        _optimal = ScoreFunction(_optimalParameters);
        Console.WriteLine($"Optimal: {_optimal}, Parameters: {_optimalParameters}");
    }

    [Fact]
    public void Bayesian() {
        using var py = Py.GIL();
        ParametersAnalyzer<Parameters>.Initialize();

        var opt = new PyBasyesianOptimization<Parameters>(ScoreFunction);
        //(double Score, Parameters Parameters) = opt.Search(n_calls: 100, 10, verbose: false);

        (double score, Parameters parameters) = opt.Search(75, 50, random_state: 1337);
        Console.WriteLine($"Best Score: {score}, Parameters: {parameters}");
        Console.WriteLine($"{_optimal - score} score from optimal");

        (_optimal - score).Should().BeLessThan(0.1).And.BeGreaterThan(0);
    }

    [Fact]
    public void Forest() {
        using var py = Py.GIL();
        ParametersAnalyzer<Parameters>.Initialize();

        var opt = new PyForestOptimization<Parameters>(ScoreFunction);
        //(double Score, Parameters Parameters) = opt.Search(n_calls: 100, 10, verbose: false);

        (double score, Parameters parameters) = opt.Search(700, 500, random_state: 1337);
        Console.WriteLine($"Best Score: {score}, Parameters: {parameters}");
        Console.WriteLine($"{_optimal - score} score from optimal");
        (_optimal - score).Should().BeLessThan(15).And.BeGreaterThan(0);
    }

    [Fact]
    public void Gbrt() {
        using var py = Py.GIL();
        ParametersAnalyzer<Parameters>.Initialize();

        var opt = new PyGbrtOptimization<Parameters>(ScoreFunction);
        //(double Score, Parameters Parameters) = opt.Search(n_calls: 100, 10, verbose: false);

        (double score, Parameters parameters) = opt.Search(700, 500, random_state: 1337);
        Console.WriteLine($"Best Score: {score}, Parameters: {parameters}");
        Console.WriteLine($"{_optimal - score} score from optimal");
        (_optimal - score).Should().BeLessThan(5).And.BeGreaterThan(0);
    }

    [Fact]
    public void Random() {
        using var py = Py.GIL();
        ParametersAnalyzer<Parameters>.Initialize();

        var opt = new PyRandomOptimization<Parameters>(ScoreFunction);
        //(double Score, Parameters Parameters) = opt.Search(n_calls: 100, 10, verbose: false);

        (double score, Parameters parameters) = opt.Search(5000, random_state: 1337);
        Console.WriteLine($"Best Score: {score}, Parameters: {parameters}");
        Console.WriteLine($"{_optimal - score} score from optimal");

        (_optimal - score).Should().BeLessThan(15).And.BeGreaterThan(0);
    }

    [Fact]
    public void ParameterCollection() {
        ParametersAnalyzer<Parameters>.Initialize();

        var types = ParametersAnalyzer<Parameters>.Parameters;

        types.Should().HaveCount(8);
        types["Seed"].Type.Should().Be(TypeCode.Int32);
        types["FloatSeed"].Type.Should().Be(TypeCode.Double);
        types["Categories"].Type.Should().Be(TypeCode.String);
        types["NumericalCategories"].Type.Should().Be(TypeCode.Single);
        types["UseMethod"].Type.Should().Be(TypeCode.Boolean);
        types["AnEnum"].Type.Should().Be(TypeCode.String);
        types["AnEnumWithValues"].Type.Should().Be(TypeCode.String);
        types["Letter"].Type.Should().Be(TypeCode.Char);

        types["Seed"].IsFloating.Should().BeFalse();
        types["FloatSeed"].IsFloating.Should().BeTrue();
        types["NumericalCategories"].IsFloating.Should().BeTrue();

        types["AnEnum"].ValueType.Should().Be(typeof(string));
        types["Letter"].ValueType.Should().Be(typeof(char));
    }
}