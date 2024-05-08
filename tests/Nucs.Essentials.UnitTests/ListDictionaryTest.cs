using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using FluentAssertions;
using Nucs.Collections;
using Xunit;
using Xunit.Abstractions;

namespace Nucs.Essentials.UnitTests;

//Keys: Ctrl + E : check what func requires, Ctrl + W : select word, Ctrl + Shift + W : unselect word
//TODO: Read about generics, warning: its a deepdive so just get the general idea and dive back to tests
public class ListDictionaryTest
{
    [Fact]
    public void AddTest()
    {
        //Arrange
        var listDictionary = new ListedDictionary<string, string>();

        //Act
        listDictionary.Add("item1", "value1");

        //Assert
        Assert.True(listDictionary.Contains(new KeyValuePair<string, string>("item1", "value1")));
        Assert.False(listDictionary.Contains(new KeyValuePair<string, string>("item2", "value2")));
    }
    
    [Fact]
    public void AddRangeTest()
    {
        //Arrange
        var tempDictionary = new ListedDictionary<string, string>();
        tempDictionary.Add("item1", "value1");
        tempDictionary.Add("item2", "value2");
        tempDictionary.Add("item3", "value3");
        
        var listDictionary = new ListedDictionary<string, string>();
        
        //Act
        listDictionary.AddRange(tempDictionary);
        
        //Assert
        Assert.True(listDictionary.Contains(new KeyValuePair<string, string>("item2", "value2")));
    }

    [Fact]
    public void ClearTest()
    {
        //Arrange
        var listDictionary = new ListedDictionary<string, string>();
        listDictionary.Add("item1", "value1");
        
        //Act
        listDictionary.Clear();
        
        //Assert
        Assert.Empty(listDictionary);
        Assert.False(listDictionary.Contains(new KeyValuePair<string, string>("item1", "value1")));
    }

    [Fact]
    public void ContainsTest()
    {
        //Arrange
        var listDictionary = new ListedDictionary<string, string>();
        listDictionary.Add("item1", "value1");
        
        //Assert
        listDictionary.Contains(new KeyValuePair<string, string>("item1", "value1")).Should().BeTrue();
        listDictionary.Contains(new KeyValuePair<string, string>("item2", "value2")).Should().BeFalse();
    }

    [Fact]
    public void CopyToTest()
    {
        //Arrange
        var listDictionary = new ListedDictionary<string, string>();
        listDictionary.Add("item1", "value1");
        listDictionary.Add("item2", "value2");
        listDictionary.Add("item3", "value3");
        var array = new KeyValuePair<string, string>[3];
        //Act
        listDictionary.CopyTo(array, 0);
        //Assert
        Assert.Equal(new KeyValuePair<string, string>("item1", "value1"), array[0]);
        Assert.Equal(new KeyValuePair<string, string>("item2", "value2"), array[1]);
        Assert.Equal(new KeyValuePair<string, string>("item3", "value3"), array[2]);
    }
    
    [Fact]
    public void RemoveTest()
    {
        //Arrange
        var listDictionary = new ListedDictionary<string, string>();
        listDictionary.Add("item1", "value1");
        
        //Act
        listDictionary.Remove(new KeyValuePair<string, string>("item1", "value1"));
        
        //Assert
        Assert.Empty(listDictionary);
        Assert.DoesNotContain(new KeyValuePair<string, string>("item1", "value1"), listDictionary);
    }

    [Fact]
    public void ContainsKeyTest()
    {
        //Arrange
        var listDictionary = new ListedDictionary<string, string>();
        listDictionary.Add("item1", "value1");
        //Act
        var resultT = listDictionary.ContainsKey("item1");
        var resultF = listDictionary.ContainsKey("item2");
        //Assert
        resultT.Should().BeTrue();
        resultF.Should().BeFalse();
    }

    [Fact]
    public void RemoveTest2()
    {
        //Arrange
        var listDictionary = new ListedDictionary<string, string>();
        listDictionary.Add("item1", "value1");
        //Act
        var result = listDictionary.Remove("item1");
        //Assert
        result.Should().BeTrue();
        listDictionary.ContainsKey("item1").Should().BeFalse();
    }

    [Fact]
    public void IndexOfTest()
    {
        //Arrange
        var listDictionary = new ListedDictionary<string, string>();
        listDictionary.Add("item1", "value1");
        listDictionary.Add("item2", "value2");
        //Act
        var result1 = listDictionary.IndexOf("item1");
        var result2 = listDictionary.IndexOf("item2");
        //Assert
        result1.Should().Be(0);
        result2.Should().Be(1);
    }

    [Fact]
    public void TryGetValueTest()
    {
        //Arrange
        var listDictionary = new ListedDictionary<string, string>();
        listDictionary.Add("item1", "value1");
        //Act
        var resultT = listDictionary.TryGetValue("item1", out var value1);
        var resultF = listDictionary.TryGetValue("item2", out var value2);
        //Assert
        resultT.Should().BeTrue();
        resultF.Should().BeFalse();
        value1.Should().Be("value1");
    }
}