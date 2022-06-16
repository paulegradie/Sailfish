using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using VeerPerforma.TestAdapter.Utils;

namespace VeerPerforma.TestAdapter
{
    // https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md
    [FileExtension(".dll")]
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    public class TestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> containers, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {

            containers = containers.Where(x => !x.StartsWith("VeerPerforma.TestAdapter"));
            
            try
            {
                var testCases = new TestDiscovery().DiscoverTests(containers);
                foreach (var testCase in testCases) discoverySink.SendTestCase(testCase);
            }
            catch (Exception ex)
            {
                logger.SendMessage(TestMessageLevel.Error, $"Encountered a bonkers error!: -- {ex.Message}");
            }
        }
    }
}