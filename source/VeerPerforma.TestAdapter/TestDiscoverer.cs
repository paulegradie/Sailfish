using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using VeerPerforma.TestAdapter.ExtensionMethods;

namespace VeerPerforma.TestAdapter;

// https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md
[FileExtension(".dll")]
[FileExtension(".cs")]
[FileExtension(".exe")]
public class TestDiscoverer : ITestDiscoverer
{
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        var testCases = sources.DiscoverTests();
        foreach (var testCase in testCases)
        {
            Console.WriteLine($"OMG WE FOUND A TEST: {testCase.DisplayName}");
            discoverySink.SendTestCase(testCase);
        }
    }
}