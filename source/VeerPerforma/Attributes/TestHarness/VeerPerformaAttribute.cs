using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace VeerPerforma.Attributes.TestHarness;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class VeerPerformaAttribute : Attribute
{
    internal VeerPerformaAttribute()
    {
    }

    public VeerPerformaAttribute(int numIterations = 3, int numWarmupIterations = 3)
    {
        NumIterations = numIterations;
    }

    public int NumIterations { get; set; }
    public int NumWarmupIterations { get; set; }
}

public class TestCollector : ITestDiscoverer
{
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        throw new NotImplementedException();
    }
}