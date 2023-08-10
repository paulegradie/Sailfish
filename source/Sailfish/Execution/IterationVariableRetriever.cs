using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal class IterationVariableRetriever : IIterationVariableRetriever
{
    public Dictionary<string, VariableAttributeMeta> RetrieveIterationVariables(Type type)
    {
        var iterationVariables = type.GetPropertiesWithAttribute<SailfishVariableAttribute>().ToList();

        // NO reason we can't support arbitrary actually - because we can always hold all other varaibles constant when analyzing one dimension (which we'll have to do)

        // if (ThereAreMoreThanTwoComplexityVariables(iterationVariables, out var numFound))
        // {
        //     throw new SailfishException($"Up to 2 complexity variables are supported. Found: {numFound}");
        // }

        return iterationVariables
            .ToDictionary(
                prop => prop.Name,
                prop =>
                    new VariableAttributeMeta(
                        prop
                            .GetCustomAttributes()
                            .OfType<SailfishVariableAttribute>()
                            .Single() // multiple prop on the attribute is false, so this shouldn't throw - we validate first to give feedback
                            .GetVariables()
                            .Distinct() // Duplicate values are currently allowed until we have an analyzer that prevents folks from providing duplicate values
                            .OrderBy(x => x)
                            .ToArray(),
                        prop
                            .GetCustomAttributes()
                            .OfType<SailfishVariableAttribute>()
                            .Single().IsComplexityVariable()));
    }

    private static bool ThereAreMoreThanTwoComplexityVariables(IEnumerable<PropertyInfo> iterationVariables, out int numFound)
    {
        numFound = iterationVariables
            .SelectMany(x => x.GetCustomAttributes<SailfishVariableAttribute>())
            .Select(x => x.IsComplexityVariable())
            .Count();
        return numFound > 2;
    }
}