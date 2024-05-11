using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using FluentAssertions;
using Nucs.Collections;
using Nucs.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nucs.Essentials.UnitTests.Resources;

public class ValueStringBuilderTest
{
    [Fact]
    public void InsertTest()
    {
        //Arrange
        var sb = new ValueStringBuilder();
        //Act
        sb.Append("Absolute Kek");
        sb.Insert(8, " Top");
        //Assert
        sb.ToString().Should().Be("Absolute Top Kek");
    }

    [Fact]
    public void AppendTest()
    {
        //Arrange
        var sb = new ValueStringBuilder();

        sb.Append("This is ");
        sb.Append("Absolute Kek");
        //Assert
        sb.ToString().Should().Be("This is Absolute Kek");

        //Arrange
        var sb2 = new ValueStringBuilder(100);
        //Assert
        sb2.Append("This is ");
        sb2.Length.Should().Be(8);
        sb2.Append('!', 5);
        sb2.Length.Should().Be(13);
        sb2.AppendSpan(5);
        sb2.Length.Should().Be(18);
        sb2.Append("a long space");
        sb2.Length.Should().Be(30);
        sb2.Insert(15, "HA");
        sb2[15].ToString().Should().Be("H");
        sb2[13] = ' ';
        sb2[14] = ' ';
        sb2[17] = ' ';
        sb2[18] = ' ';
        sb2[19] = ' ';
        sb2.ToString().Should().Be("This is !!!!!  HA   a long space");
    }

    [Fact]
    public void AppendTest5()
    {
        //Arrange
        var sb = new ValueStringBuilder();
        
        //Assert
        sb.Append("This is ");
        sb.ToString().Should().Be("This is ");
        sb.Insert(0, "A");
        sb.ToString().Should().Be("AThis is ");
        sb.Insert(sb.Length, "B");
        sb.ToString().Should().Be("AThis is B");
        sb.Insert(1, "C");
        sb.ToString().Should().Be("ACThis is B");
        
        new Action(AppendTest25).Should().Throw<ArgumentOutOfRangeException>();
        void AppendTest25()
        {
            var sb = new ValueStringBuilder();
            sb.Append("This is ");
            sb.Insert(-1, "A"); //throws exception
        }
    }

    [Fact]
    public void TryCopyToTest()
    {
        //Arrange
        var sb = new ValueStringBuilder();
        sb.Append("Some text in a nice string builder");

        Span<char> destination = new char[34];
        Span<char> destination2 = new char[20];
        Span<char> destination3 = new char[100];
        
        //Assert
        sb.TryCopyTo(destination, out int charsWritten).Should().BeTrue();
        charsWritten.Should().Be(34);
        //Assert
        sb.Append("This is another text after the first one got demolished");
        sb.TryCopyTo(destination2, out int charsWritten2).Should().BeFalse();
        charsWritten2.Should().Be(0);
        //Assert
        sb.TryCopyTo(destination3, out int charsWritten3).Should().BeTrue();
        charsWritten3.Should().Be(0);
    }

    [Theory]
    [InlineData("Hello World", 11, 16)]
    [InlineData("Hello World, This is a longer sentence", 38, 64)]
    [InlineData("Shorter, but longer than 1st", 28, 32)]
    public void AppendCapacityTest(string input, int lengthOutput, int capacityOutput)
    {
        //Arrange
        var sb = new ValueStringBuilder();
        //Act
        sb.Append(input);
        //Assert
        sb.Length.Should().Be(lengthOutput);
        sb.Capacity.Should().Be(capacityOutput);
    }


    [Theory]
    [InlineData(8, 16)]
    [InlineData(17, 32)]
    [InlineData(33, 64)]
    [InlineData(65, 128)]
    public void EnsureTotalCapacityTest(int required, int expected)
    {
        //Arrange
        var sb = new ValueStringBuilder();
        sb.Append("Heyyyyy");
        //Act
        sb.EnsureTotalCapacity(required);
        //Assert
        sb.Capacity.Should().Be(expected);
    }
    
    [Fact]
    public void RemoveStartTest()
    {
        //Arrange
        var sb = new ValueStringBuilder();
        sb.Append("Hello World");
        //Act
        sb.RemoveStart(6);
        //Assert
        sb.ToString().Should().Be("World");
        //Act
        sb.RemoveStart(5);
        //Assert
        sb.ToString().Should().Be("");
        //Act
        sb.Append("Hello");
        sb.RemoveStart(10);
        //Assert
        sb.ToString().Should().Be("");
    }
    
    [Theory]
    [InlineData("Hello World", 5)]
    [InlineData("Hello World, This is a longer sentence", 26)]
    [InlineData("Shorter, but longer than 1st", 4)]
    [InlineData("Capacity can be 16, 32, 64 and so on. This one is 64", 12)]
    [InlineData("This string is 11 chars long, therefore the capacity is 128 and buffer is 52", 52)]
    public void GetBufferTest(string input, int bufferLength)
    {
        //Arrange
        var sb = new ValueStringBuilder();
        sb.Append(input);
        //Act
        var buffer = sb.GetBuffer();
        //Assert
        buffer.Length.Should().Be(bufferLength);
    }
}