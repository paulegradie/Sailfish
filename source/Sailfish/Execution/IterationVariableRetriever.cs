using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal class IterationVariableRetriever : IIterationVariableRetriever
{
    public Dictionary<string, object[]> RetrieveIterationVariables(Type type)
    {
        var dict = new Dictionary<string, object[]>();
        var propertiesWithAttribute = type.GetPropertiesWithAttribute<SailfishVariableAttribute>();
        foreach (var property in propertiesWithAttribute)
        {
            var variableValues = property
                .GetCustomAttributes()
                .OfType<SailfishVariableAttribute>()
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