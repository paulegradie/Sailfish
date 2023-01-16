using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.TestAdapter.Utils;

namespace Sailfish.TestAdapter;

// https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md
[FileExtension(".dll")]
[DefaultExecutorUri(TestExecutor.ExecutorUriString)]
public class TestDiscoverer : ITestDiscoverer
{
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        sources = sources.Where(x => !x.StartsWith("Sailfish.TestAdapter"));
        CustomLogger.VerbosePadded("TEST SOURCES IN TESTDISCOVERER: {TESTS}", string.Join(", ", sources));
        try
        {
            var testCases = TestDiscovery.DiscoverTests(sources);
            foreach (var testCase in testCases)
            {
                
                discoverySink.SendTestCase(testCase);
            }
        }
        catch (Exception ex)
        {
            logger.SendMessage(TestMessageLevel.Error, $"Encountered a bonkers error!: -- {ex.Message}");
        }
    }
}