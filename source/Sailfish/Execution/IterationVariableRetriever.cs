using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;

namespace Sailfish.Execution;

internal interface IIterationVariableRetriever
{
    Dictionary<string, VariableAttributeMeta> RetrieveIterationVariables(Type type);
}

internal class IterationVariableRetriever : IIterationVariableRetriever
{
    public Dictionary<string, VariableAttributeMeta> RetrieveIterationVariables(Type type)
    {
        var result = new Dictionary<string, VariableAttributeMeta>();

        // Handle attribute-based variables (existing system)
        var attributeProperties = type.CollectAllSailfishVariableAttributes();
        foreach (var prop in attributeProperties)
        {
            var variableProvider = new AttributeVariableProvider(
                prop.GetCustomAttributes()
                    .Where(x => x.IsSailfishVariableAttribute())
                    .Cast<ISailfishVariableAttribute>()
                    .Single());

            result[prop.Name] = new VariableAttributeMeta(
                [
                    .. variableProvider
                        .GetVariables()
                        .Distinct() // Duplicate values are currently allowed until we have an analyzer that prevents folks from providing duplicate values
                        .OrderBy(x => x)
                ],
                variableProvider.IsScaleFishVariable());
        }



        // Handle interface-based typed variables (ISailfishVariables<TType, TTypeProvider> system)
        var typedVariableProperties = type.CollectAllSailfishVariablesProperties();
        foreach (var prop in typedVariableProperties)
        {
            var variableProvider = new TypedVariableProvider(prop.PropertyType);

            result[prop.Name] = new VariableAttributeMeta(
                [
                    .. variableProvider
                        .GetVariables()
                        .Distinct()
                        .OrderBy(x => x)
                ],
                variableProvider.IsScaleFishVariable());
        }

        return result;
    }
}