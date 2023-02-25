using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal interface IIterationVariableRetriever
{
    Dictionary<string, T[]> RetrieveIterationVariables<T>(Type type);
}

internal class IterationVariableRetriever : IIterationVariableRetriever
{
    public Dictionary<string, T[]> RetrieveIterationVariables<T>(Type type)
    {
        var dict = new Dictionary<string, T[]>();
        var propertiesWithAttribute = type.GetPropertiesWithAttribute<SailfishVariableAttribute<T>>();
        foreach (var property in propertiesWithAttribute)
        {
            var variableValues = property
                .GetCustomAttributes()
                .OfType<SailfishVariableAttribute<T>>()
                .Single() // multiple prop on the attribute is false, so this shouldn't throw - we validate first to give feedback
                .GetVariables()
                .Distinct() // Duplicate values are currently allowed until we have an analyzer that prevents folks from providing duplicate values
                .OrderBy(x => x)
                .ToArray();
            dict.Add(property.Name, variableValues);
        }

        return dict;
    }
}