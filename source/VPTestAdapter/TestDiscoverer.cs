using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using VeerPerforma.Attributes.TestHarness;

namespace VPTestAdapter;

// example test adapter!
// https://github.com/brunolm/TSTestExtension
// More examples!
// https://stackoverflow.com/questions/21646104/how-do-i-get-visual-studios-test-window-to-use-my-itestcontainerdiscoverer
[FileExtension(".dll")]
public class TestDiscoverer : ITestDiscoverer
{
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        // basically what needs to happen in this method is for us to take our IEnumerable of files (which are basically all the files in
        // the current assembly
        // filter out the files that don't have tests
        // and then pass those to the ITestCaseDiscovery sink
        // the sink is basically the whole that you will drop ITestCase's into which will be provided to 
        // the test controls.
        // at its most very basic (assuming all you have are test files)
        var testCases = sources.DiscoverTests();
        foreach (var testCase in testCases)
        {
            discoverySink.SendTestCase(testCase);
        }
    }
}