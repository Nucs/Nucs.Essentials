using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using FluentAssertions;
using Nucs.Collections;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nucs.Essentials.UnitTests;

public class RollingWindowTest
{
    //TODO: try to do more tests with args cuz its awseome
    [Theory]
    [InlineData(1, 2, 0)]
    [InlineData(2, 2, 1)]
    [InlineData(3, 2, 2)]
    [InlineData(1, 3, 0)]
    [InlineData(3, 3, 2)]
    [InlineData(4, 3, 3)]
    public void MySpecialTestNewest(int numberOfPushes, int windowSize, int newestExpectedToBe)
    {
        var window = new RollingWindow<int>(windowSize);
        for (int i = 0; i < numberOfPushes; i++)
        {
            window.Push(i);
        }
        
        window.Newest.Should().Be(newestExpectedToBe);
    }
    
    [Fact]
    public void LatestTest()
    {
        //Arrange
        var window = new RollingWindow<int>(5);
        window.Push(1);
        var latest = window.Latest;
        
        //Assert
        latest.Should().Be(1);
        
        window.Push(2);
        latest = window.Latest;
        latest.Should().Be(1);
        
        window.Push(3);
        latest = window.Latest;
        latest.Should().Be(1);
        
        //When empty
        var ex = Assert.Throws<DivideByZeroException>(() => {;
            window.Reset();
            latest = window.Latest;
        });
        
        //Arrange
        for(int i = 0; i < 8; i++)
            window.Push(i);
        //Assert
        latest = window.Latest;
        latest.Should().Be(3);
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3)]
    public void NewestTest(int number)
    {
        //Arrange
        var window = new RollingWindow<int>(3);
        
        //When Empty
        var ex = Assert.Throws<DivideByZeroException>(() => {;
            var newest = window.Newest;
        });
        //Act
        window.Push(number);
        var newest = window.Newest;
        //Assert
        newest.Should().Be(number);
    }
    
    [Fact]
    public void MostRecentlyRemovedTest()
    {
        //Arrange
        var window = new RollingWindow<int>(3);
        //When Nothing was Removed
        window.Push(1);
        Assert.Throws<InvalidOperationException>(() => {;
            var exception = window.MostRecentlyRemoved;
        });
        //Act
        window.Push(2);
        window.Push(3);
        window.Push(4);
        //Assert
        window.MostRecentlyRemoved.Should().Be(1);
        
        //Arrange
        window.Reset();
        window.Push(10);
        //When Nothing was Removed
        Assert.Throws<InvalidOperationException>(() => {;
            var exception = window.MostRecentlyRemoved;
        });
        //Act
        window.Push(20);
        window.Push(30);
        window.Push(40);
        //Assert
        window.MostRecentlyRemoved.Should().Be(10);

    }
    
    [Fact]
    public void IsReadyTest()
    {
        //Arrange
        var window = new RollingWindow<int>(3);
        //Assert
        window.IsReady.Should().BeFalse();
        
        //Arrange
        window.Push(1);
        window.Push(2);
        
        //Assert
        window.IsReady.Should().BeFalse();

        //Arrange
        window.Push(3);
        
        //Assert
        window.IsReady.Should().BeTrue();
        
        //Arrange
        window.Push(4);
        
        //Assert
        window.IsReady.Should().BeTrue();
    }

    [Fact]
    public void ThisTest()
    {
        //Arrange
        var window = new RollingWindow<int>(5);
        //Out of Range (No value)
        Assert.Throws<ArgumentOutOfRangeException>(() => {;
            _ = window[2];
        });
        //Act
        window.Push(1);
        window.Push(2);
        window.Push(3);
        window.Push(4);
        window.Push(5);
        //Assert
        window[0].Should().Be(5);
        window[3].Should().Be(2);
        //Out of Range
        Assert.Throws<ArgumentOutOfRangeException>(() => {;
            _ = window[15];
        });
    }

    [Fact]
    public void PushTest()
    {
        //Arrange
        var window = new RollingWindow<int>(3);
        //Act
        window.Push(1);
        window.Push(2);
        //Assert
        window.Newest.Should().Be(2);
        window.Latest.Should().Be(1);
        //Arrange
        var window2 = new RollingWindow<string>(3);
        //Act
        window2.Push("One");
        window2.Push("Two");
        //Assert
        window2[0].Should().Be("Two");
        //Act
        window2.Push("Three");
        window2.Push("Four");
        //Assert
        window2.Latest.Should().Be("Two");
    }

    [Fact]
    public void ResetTest()
    {
        //Arrange
        var window = new RollingWindow<int>(3);
        //Act
        window.Push(1);
        window.Push(2);
        window.Push(3);
        window.Reset();
        //Assert
        window.Count.Should().Be(0);
        window.IsReady.Should().BeFalse();
        //Act
        window.Push(2);
        window.Push(5);
        window.Reset();
        window.Push(1);
        //Assert
        window.Count.Should().Be(1);
    }
}