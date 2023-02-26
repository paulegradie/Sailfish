using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Execution;

internal class PropertySetGenerator : IPropertySetGenerator
{
    private readonly IParameterCombinator parameterCombinator;
    private readonly IIterationVariableRetriever iterationVariableRetriever;

    public PropertySetGenerator(IParameterCombinator parameterCombinator, IIterationVariableRetriever iterationVariableRetriever)
    {
        this.parameterCombinator = parameterCombinator;
        this.iterationVariableRetriever = iterationVariableRetriever;
    }

    public IEnumerable<PropertySet> GeneratePropertySets(Type test)
    {
        var variableProperties = iterationVariableRetriever.RetrieveIterationVariables(test);

        var propNames = new List<string>();
        var propValues = new List<List<dynamic>>();
        foreach (var (propertyName, values) in variableProperties)
        {
            propNames.Add(propertyName);
            propValues.Add(values.ToList());
        }

        var propertySets = parameterCombinator.GetAllPossibleCombos(propNames, propValues);

        return propertySets;
    }
}

internal static class ToDynamicExtensionMethods
{
    public static Dictionary<string, dynamic[]> ToDynamic(this Dictionary<string, string[]> dict)
    {
        return dict.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(x => (dynamic)x).ToArray()
        );
    }

    public static Dictionary<string, dynamic[]> ToDynamic(this Dictionary<string, int[]> dict)
    {
        return dict.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(x => (dynamic)x).ToArray()
        );
    }

    public static Dictionary<string, dynamic[]> ToDynamic(this Dictionary<string, List<dynamic>[]> dict)
    {
        return dict.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(x => (dynamic)x).ToArray()
        );
    }

    public static Dictionary<string, dynamic[]> ToDynamic(this Dictionary<string, dynamic[][]> dict)
    {
        return dict.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(x => (dynamic)x).ToArray()
        );
    }

    public static Dictionary<string, dynamic[]> ToDynamic(this Dictionary<string, Dictionary<string, dynamic>[]> dict)
    {
        return dict.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(x => (dynamic)x).ToArray()
        );
    }
}