using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal class TestCollector : ITestCollector
{
    public IEnumerable<Type> CollectTestTypes(IEnumerable<Type> sourceTypes)
    {
        var enumerable = sourceTypes.ToList();
        if (!enumerable.Any())
        {
            return CollectTestTypes();
        }

        var allTests = new List<Type>();
        foreach (var sourceType in enumerable)
        {
            var testTypes = sourceType.Assembly.GetTypes().Where(t => t.HasAttribute<SailfishAttribute>());
            allTests.AddRange(testTypes);
        }

        return allTests.Distinct().ToArray();
    }

    private static IEnumerable<Type> CollectTestTypes()
    {
        var types = Assembly.GetCallingAssembly().GetTypes().Where(t => t.HasAttribute<SailfishAttribute>()).ToArray();
        return types;
    }
}