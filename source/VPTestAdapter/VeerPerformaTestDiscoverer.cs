using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace VPTestAdapter;

// example test adapter!
// https://github.com/brunolm/TSTestExtension
// More examples!
// https://stackoverflow.com/questions/21646104/how-do-i-get-visual-studios-test-window-to-use-my-itestcontainerdiscoverer
[FileExtension(".cs")]
public class VeerPerformaTestDiscoverer : ITestDiscoverer
{
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        throw new NotImplementedException();
    }
}