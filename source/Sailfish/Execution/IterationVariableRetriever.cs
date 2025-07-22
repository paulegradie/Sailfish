using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;

namespace Sailfish.Execution;

internal interface IIterationVariableRetriever
{
    Dictionary<string, VariableAttributeMeta> RetrieveIterationVariables(Type type);
}

internal class IterationVariableRetriever : IIterationVariableRetriever
{
    public Dictionary<string, VariableAttributeMeta> RetrieveIterationVariables(Type type)
    {
        var attributeBasedVariables = GetAttributeBasedVariables(type);
        var interfaceBasedVariables = GetInterfaceBasedVariables(type);
        var complexVariables = GetComplexVariables(type);

        // Combine all types of variables
        var allVariables = new Dictionary<string, VariableAttributeMeta>();

        foreach (var kvp in attributeBasedVariables)
        {
            allVariables[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in interfaceBasedVariables)
        {
            allVariables[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in complexVariables)
        {
            allVariables[kvp.Key] = kvp.Value;
        }

        return allVariables;
    }

    private Dictionary<string, VariableAttributeMeta> GetAttributeBasedVariables(Type type)
    {
        return type
            .CollectAllSailfishVariableAttributes()
            .ToDictionary(
                prop => prop.Name,
                prop =>
                    new VariableAttributeMeta(
                        [
                            .. prop
                                .GetCustomAttributes()
                                .Where(x => x.IsSailfishVariableAttribute())
                                .Cast<ISailfishVariableAttribute>().Single() // multiple prop on the attribute is false, so this shouldn't throw - we validate first to give feedback
                                .GetVariables()
                                .Distinct() // Duplicate values are currently allowed until we have an analyzer that prevents folks from providing duplicate values
                                .OrderBy(x => x)
                        ],
                        prop
                            .GetCustomAttributes()
                            .Where(x => x.IsSailfishVariableAttribute())
                            .Cast<ISailfishVariableAttribute>()
                            .Single()
                            .IsScaleFishVariable()));
    }

    private Dictionary<string, VariableAttributeMeta> GetInterfaceBasedVariables(Type type)
    {
        return type
            .CollectAllSailfishVariablesProperties()
            .ToDictionary(
                prop => prop.Name,
                prop =>
                {
                    var provider = new TypedVariableProvider(prop.PropertyType);
                    var variables = provider.GetVariables()
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();

                    return new VariableAttributeMeta(
                        variables,
                        provider.IsScaleFishVariable());
                });
    }

    private Dictionary<string, VariableAttributeMeta> GetComplexVariables(Type type)
    {
        return type
            .CollectAllComplexVariableProperties()
            .ToDictionary(
                prop => prop.Name,
                prop =>
                {
                    var provider = new ComplexVariableProvider(prop.PropertyType);
                    var variables = provider.GetVariables()
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();

                    return new VariableAttributeMeta(
                        variables,
                        provider.IsScaleFishVariable());
                });
    }
}