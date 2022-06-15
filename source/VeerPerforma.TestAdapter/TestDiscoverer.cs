using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using VeerPerforma.TestAdapter.Utils;

namespace VeerPerforma.TestAdapter
{
    // https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md
    [FileExtension(".dll")]
    [FileExtension(".cs")]
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    public class TestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> containers, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger.SendMessage(TestMessageLevel.Informational, "THIS IS A TEST MESSAGE LEVEL");
            logger.SendMessage(TestMessageLevel.Warning, "THIS IS A WARNING");
            logger.SendMessage(TestMessageLevel.Error, "OOPSIE - THIS IS AN ERROR");


            
            var testCases = new CustomTestDiscovery().DiscoverTests(containers);
            foreach (var testCase in testCases) discoverySink.SendTestCase(testCase);
        }
    }
}