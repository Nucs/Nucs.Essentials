using System;
using System.Linq;
using FluentAssertions;
using Nucs.Optimization;
using Xunit;
using Xunit.Abstractions;

namespace Nucs.Essentials.UnitTests; 


public class LoadDumpTests : PythonTest {
    private readonly ITestOutputHelper _testOutputHelper;
    public LoadDumpTests(ITestOutputHelper testOutputHelper) {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void LoadAndDump() {
        //run a PyBayesianOptimization and then dump it to a temporary file
        using var tmpFile = new TempFile();
        _testOutputHelper.WriteLine(tmpFile.ToString());
        var bo = new PyBasyesianOptimization<Parameters>(BlackBoxScoreFunction, maximize: true, tmpFile);
        bo.SearchTop(20, 20, 10);

        var loaded = PyOptimization<Parameters>.Load(tmpFile, maximize: true);
        loaded.Iterations.Length.Should().Be(20);
        loaded.Iterations.All(i => i.Score != 0).Should().BeTrue();
        loaded.Best.Equals(loaded.Iterations.MaxBy(b=>b.Score).Parameters).Should().BeTrue();
    }

    private double BlackBoxScoreFunction(Parameters parameters) {
        var res = new Random(parameters.Seed).Next(1, int.MaxValue);
        Console.WriteLine($"Score: {res}, Parameters: {parameters}");
        return res;
    }
}