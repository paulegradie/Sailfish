using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Newtonsoft.Json;
using Sailfish.TestAdapter.Discovery;

namespace Sailfish.TestAdapter;

// https://github.com/Microsoft/vstest-docs/blob/main/RFCs/0004-Adapter-Extensibility.md
[FileExtension(".dll")]
[DefaultExecutorUri(TestExecutor.ExecutorUriString)]
public class TestDiscoverer : ITestDiscoverer
{
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        sources = sources.ToList();
        var filteredSource = sources.Where(x => !x.EndsWith("Sailfish.TestAdapter.dll") && !x.EndsWith("Tests.Sailfish.TestAdapter.dll"));
        try
        {
            var testCases = TestDiscovery.DiscoverTests(filteredSource, logger).ToList();
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