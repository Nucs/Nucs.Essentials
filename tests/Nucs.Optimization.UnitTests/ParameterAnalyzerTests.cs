using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;
using DotNext.Threading.Tasks;
using FluentAssertions;
using Nucs.Optimization.Analayzer;
using Nucs.Threading;
using Xunit;
using AsyncCountdownEvent = Nucs.Threading.AsyncCountdownEvent;

namespace Nucs.Essentials.UnitTests;

public class ParameterAnalyzerTests {
    [Fact]
    public void ApplyAndSort() {
        ParametersAnalyzer<Parameters>.Initialize();

        var parameters = new Parameters();
        var list = new List<(string, object)> {
            ("Seed", (object) 1),
            ("FloatSeed", (object) 1.5),
            ("Categories", (object) "A"),
            ("NumericalCategories", (object) 1),
            ("UseMethod", (object) true),
            ("AnEnum", (object) "A"),
            ("AnEnumWithValues", (object) "A"),
            ("Letter", (object) 'a')
        };

        ParametersAnalyzer<Parameters>.Apply(parameters, list, sortFirst: true);

        parameters.Seed.Should().Be(1);
        parameters.FloatSeed.Should().Be(1.5);
        parameters.Categories.Should().Be("A");
        parameters.NumericalCategories.Should().Be(1);
        parameters.UseMethod.Should().BeTrue();
        parameters.AnEnum.Should().Be(SomeEnum.A);
        parameters.AnEnumWithValues.Should().Be(SomeEnum.A);
        parameters.Letter.Should().Be('a');
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

        types["AnEnum"].ValueType.Should().Be(typeof(SomeEnum));
        types["Letter"].ValueType.Should().Be(typeof(char));
    }
}