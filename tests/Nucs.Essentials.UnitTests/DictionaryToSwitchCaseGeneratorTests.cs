using System;
using System.Collections.Generic;
using Nucs.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Nucs.Essentials.UnitTests; 

public class DictionaryToSwitchCaseGeneratorTests {
    private readonly ITestOutputHelper Console;
    public DictionaryToSwitchCaseGeneratorTests(ITestOutputHelper console) {
        Console = console;
    }

    [Fact]
    public void StringIntDictionary_Reverse() {
        var dictionary = new Dictionary<string, int>
        {
            { "One", 1 },
            { "Two", 2 },
            { "Three", 3 }
        };

        // Convert the dictionary to a Func<int, string>
        var func = DictionaryToSwitchCaseGenerator.CreateLookupFunc(dictionary);

        // Test the func
        Console.WriteLine(func("One").ToString()); // Outputs 1
        Console.WriteLine(func("Two").ToString()); // Outputs 2
        Console.WriteLine(func("Three").ToString()); // Outputs 3
    }
    
    [Fact]
    public void StringIntDictionary() {
        var dictionary = new Dictionary<string, int>
        {
            { "One", 1 },
            { "Two", 2 },
            { "Three", 3 }
        };

        // Convert the dictionary to a Func<int, string>
        var func = DictionaryToSwitchCaseGenerator.CreateReversedLookupFunc(dictionary);
        
        // Test the func
        Console.WriteLine(func(1)); // Outputs "One"
        Console.WriteLine(func(2)); // Outputs "Two"
        Console.WriteLine(func(3)); // Outputs "Three"
    }
    
    [Fact]
    public void IntStringDictionary() {
        var dictionary = new Dictionary<int, string>
        {
            { 1, "One" },
            { 2, "Two" },
            { 3, "Three" }
        };

        // Convert the dictionary to a Func<int, string>
        var func = DictionaryToSwitchCaseGenerator.CreateLookupFunc(dictionary);

        // Test the func
        Console.WriteLine(func(1)); // Outputs "One"
        Console.WriteLine(func(2)); // Outputs "Two"
        Console.WriteLine(func(3)); // Outputs "Three"
    }
}