using System.Reflection;
using VeerPerforma.Attributes.TestHarness;
using VeerPerforma.Utils.Discovery;

namespace VeerPerforma.Executor.Prep;

public class TestCollector : ITestCollector
{
    public Type[] CollectTestTypes()
    {
        return Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.HasAttribute<VeerPerformaAttribute>())
            .ToArray();
    }
}