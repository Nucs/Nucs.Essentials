using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DotNext.Threading;
using DotNext.Threading.Tasks;
using FluentAssertions;
using Nucs.Essentials.UnitTests.Resources;
using Nucs.Optimization;
using Nucs.Optimization.Analyzer;
using Nucs.Optimization.Attributes;
using Nucs.Optimization.Callbacks;
using Nucs.Reflection;
using Nucs.Threading;
using Python.Runtime;
using Xunit;
using Xunit.Abstractions;
using AsyncCountdownEvent = Nucs.Threading.AsyncCountdownEvent;

namespace Nucs.Essentials.UnitTests;

public class OptimizationCallbackTests : PythonTest {
    private readonly ITestOutputHelper Console;

    public OptimizationCallbackTests(ITestOutputHelper console) {
        Console = console;
    }

    [Maximize]
    double ScoreFunction(Parameters parameters) {
        /** Math.Min(0.5, Math.Max(0.00000001, parameters.FloatSeed - 0.75)*/
        var res = (parameters.Seed * parameters.NumericalCategories * (parameters.UseMethod ? 1 : -1) * Math.Sin(0.05 + parameters.FloatSeed)) / 1000000;
        Console.WriteLine($"Score: {res}, Parameters: {parameters}");
        return res;
    }

    [Fact]
    public void Random_IterationCallback() {
        using var _ = Py.GIL();

        ParametersAnalyzer<Parameters>.Initialize();
        StrongBox<int> counter = new(0);

        void Callback(int iterations, Parameters parameters, double score) {
            counter.Value++;
        }

        var opt = new PyRandomOptimization<Parameters>(ScoreFunction);
        //(double Score, Parameters Parameters) = opt.Search(n_calls: 100, 10, verbose: false);

        (double score, Parameters parameters) = opt.Search(5000, random_state: 1337, callbacks: new[] { new IterationCallback<Parameters>(true, Callback) });
        counter.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Random_EarlyStopper_Callback() {
        using var _ = Py.GIL();

        ParametersAnalyzer<Parameters>.Initialize();
        StrongBox<int> counter = new(0);

        bool Callback(Parameters parameters, double score) {
            return ++counter.Value == 100;
        }

        var opt = new PyRandomOptimization<Parameters>(ScoreFunction);
        //(double Score, Parameters Parameters) = opt.Search(n_calls: 100, 10, verbose: false);

        (double score, Parameters parameters) = opt.Search(5000, random_state: 1337, callbacks: new[] { new EarlyStopper<Parameters>(true, Callback) });
        counter.Value.Should().Be(100);
    }

    [Fact]
    public void Random_DeadlineStopper() {
        using var _ = Py.GIL();

        ParametersAnalyzer<Parameters>.Initialize();
        StrongBox<int> counter = new(0);

        void Callback(int iterations, Parameters parameters, double score) =>
            ++counter.Value;

        var opt = new PyRandomOptimization<Parameters>(ScoreFunction);

        (double score, Parameters parameters) = opt.Search(5000, random_state: 1337, callbacks: new PyOptCallback[] { new DeadlineStopper(TimeSpan.FromSeconds(0.5)), new IterationCallback<Parameters>(true, Callback) });
        Console.WriteLine(counter.Value.ToString());
        counter.Value.Should().BeLessThan(5000);
    }

    [Fact]
    public void Bayesian_DeltaX() {
        using var _ = Py.GIL();

        ParametersAnalyzer<Parameters>.Initialize();
        StrongBox<int> counter = new(0);

        void Callback(int iterations, Parameters parameters, double score) =>
            ++counter.Value;

        var opt = new PyBayesianOptimization<Parameters>(ScoreFunction);

        (double score, Parameters parameters) = opt.Search(150, 100, random_state: 1337, callbacks: new PyOptCallback[] {
            new DeltaXStopper(1),
            new IterationCallback<Parameters>(true, Callback)
        });
        Console.WriteLine(counter.Value.ToString());
        counter.Value.Should().BeLessThan(140, "Must finish earlier");
    }
}