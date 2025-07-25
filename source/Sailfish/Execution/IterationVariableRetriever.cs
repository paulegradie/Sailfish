using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Exceptions;

namespace Sailfish.Execution;

internal interface IIterationVariableRetriever
{
    Dictionary<string, VariableAttributeMeta> RetrieveIterationVariables(Type type);
}

internal class IterationVariableRetriever : IIterationVariableRetriever
{
    public Dictionary<string, VariableAttributeMeta> RetrieveIterationVariables(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        var attributeBasedVariables = GetAttributeBasedVariables(type);
        var interfaceBasedVariables = GetInterfaceBasedVariables(type);
        var classBasedVariables = GetClassBasedVariables(type);
        // Combine all types of variables with duplicate detection
        var variableSources = new[]
        {
            ("attribute-based", attributeBasedVariables),
            ("interface-based", interfaceBasedVariables),
            ("class-based", classBasedVariables)
        };

        var allVariables = new Dictionary<string, VariableAttributeMeta>();

        foreach (var (sourceType, variables) in variableSources)
        {
            foreach (var kvp in variables)
            {
                if (allVariables.ContainsKey(kvp.Key))
                {
                    throw new SailfishException(
                        $"Duplicate variable property name '{kvp.Key}' found. " +
                        $"Property names must be unique across all variable types. " +
                        $"Conflict between existing variable and {sourceType} variable.");
                }
                allVariables[kvp.Key] = kvp.Value;
            }
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

    private Dictionary<string, VariableAttributeMeta> GetClassBasedVariables(Type type)
    {
        return type
            .CollectAllSailfishVariablesClassProperties()
            .ToDictionary(
                prop => prop.Name,
                prop =>
                {
                    var provider = new SailfishVariablesClassProvider(prop.PropertyType);
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