using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;

namespace Sailfish.Execution;

internal class IterationVariableRetriever : IIterationVariableRetriever
{
    public Dictionary<string, VariableAttributeMeta> RetrieveIterationVariables(Type type)
    {
        return type
            .CollectAllSailfishVariableAttributes()
            .ToDictionary(
                prop => prop.Name,
                prop =>
                    new VariableAttributeMeta(
                        prop
                            .GetCustomAttributes()
                            .Where(x => x.IsSailfishVariableAttribute())
                            .Cast<ISailfishVariableAttribute>().Single() // multiple prop on the attribute is false, so this shouldn't throw - we validate first to give feedback
                            .GetVariables()
                            .Distinct() // Duplicate values are currently allowed until we have an analyzer that prevents folks from providing duplicate values
                            .OrderBy(x => x)
                            .ToArray(),
                        prop
                            .GetCustomAttributes()
                            .Where(x => x.IsSailfishVariableAttribute())
                            .Cast<ISailfishVariableAttribute>()
                            .Single()
                            .IsScaleFishVariable()));
    }
}