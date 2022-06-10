using System.Reflection;
using VeerPerforma.Attributes;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution;

public class TestCollector : ITestCollector
{
    public Type[] CollectTestTypes(params Type[] sourceTypes)
    {
        if (sourceTypes.Length == 0)
            return CollectTestTypes();

        var allTests = new List<Type>();
        foreach (var sourceType in sourceTypes)
        {
            var allTypes = sourceType.Assembly.GetTypes().Where(t => t.HasAttribute<VeerPerformaAttribute>());
            allTests.AddRange(allTypes);
        }

        return allTests.Distinct().ToArray();
    }

    public Type[] CollectTestTypes()
    {
        var types = Assembly.GetCallingAssembly().GetTypes().Where(t => t.HasAttribute<VeerPerformaAttribute>()).ToArray();
        return types;
    }
}