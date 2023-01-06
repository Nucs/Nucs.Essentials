using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Nucs.Reflection;

public class DictionaryToSwitchCaseGenerator {
    /// <summary>
    ///     Creates a lookup via switch-case to given <paramref name="dictionary"/>
    /// </summary>
    public static Func<TKey, TValue> CreateLookupFunc<TKey, TValue>(IDictionary<TKey, TValue> dictionary) {
        return CreateLookupExpression(dictionary).Compile();
    }

    /// <summary>
    ///     Creates a lookup via switch-case to given <paramref name="dictionary"/> for TValue of TKey
    /// </summary>
    public static Func<TValue, TKey> CreateReversedLookupFunc<TKey, TValue>(IDictionary<TKey, TValue> dictionary) {
        return CreateReversedLookupExpression(dictionary).Compile();
    }

    /// <summary>
    ///     Creates a lookup via switch-case to given <paramref name="dictionary"/>
    /// </summary>
    public static Expression<Func<TKey, TValue>> CreateLookupExpression<TKey, TValue>(IDictionary<TKey, TValue> dictionary) {
        // Create a parameter for the TValue value
        var valueParam = Expression.Parameter(typeof(TKey), "value");

        // Create a list of switch cases
        var cases = dictionary.Select(kvp =>
                                          Expression.SwitchCase(
                                              Expression.Constant(kvp.Value),
                                              Expression.Constant(kvp.Key)
                                          )
        );

        // Create a default case
        var defaultCase = Expression.Default(typeof(TValue));

        // Create the switch expression
        var switchExpr = Expression.Switch(valueParam, defaultCase, cases.ToArray());

        // Create a lambda expression from the switch expression and the value parameter
        var lambdaExpr = Expression.Lambda<Func<TKey, TValue>>(switchExpr, valueParam);

        // Compile the lambda expression to a delegate and return it
        return lambdaExpr;
    }

    /// <summary>
    ///     Creates a lookup via switch-case to given <paramref name="dictionary"/> for TValue of TKey
    /// </summary>
    public static Expression<Func<TValue, TKey>> CreateReversedLookupExpression<TKey, TValue>(IDictionary<TKey, TValue> dictionary) {
        // Create a parameter for the TValue value
        var valueParam = Expression.Parameter(typeof(TValue), "value");

        // Create a list of switch cases
        var cases = dictionary.Select(kvp =>
                                          Expression.SwitchCase(
                                              Expression.Constant(kvp.Key),
                                              Expression.Constant(kvp.Value)
                                          )
        );

        // Create a default case
        var defaultCase = Expression.Default(typeof(TKey));

        // Create the switch expression
        var switchExpr = Expression.Switch(valueParam, defaultCase, cases.ToArray());

        // Create a lambda expression from the switch expression and the value parameter
        var lambdaExpr = Expression.Lambda<Func<TValue, TKey>>(switchExpr, valueParam);

        // Compile the lambda expression to a delegate and return it
        return lambdaExpr;
    }
}