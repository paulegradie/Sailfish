using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using VeerPerforma.TestAdapter.ExtensionMethods;

namespace VeerPerforma.TestAdapter;

// example test adapter!
// https://github.com/brunolm/TSTestExtension
// More examples!
// https://stackoverflow.com/questions/21646104/how-do-i-get-visual-studios-test-window-to-use-my-itestcontainerdiscoverer
[FileExtension(".dll")]
public class TestDiscoverer : ITestDiscoverer
{
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        var testCases = sources.DiscoverTests();
        foreach (var testCase in testCases)
        {
            discoverySink.SendTestCase(testCase);
        }
    }
}