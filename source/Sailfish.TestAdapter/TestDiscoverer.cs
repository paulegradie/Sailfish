using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Exceptions;
using Sailfish.TestAdapter.Discovery;

namespace Sailfish.TestAdapter;

[FileExtension(".dll")]
[DefaultExecutorUri(TestExecutor.ExecutorUriString)]
// ReSharper disable once UnusedType.Global
public class TestDiscoverer : ITestDiscoverer
{
    private readonly List<string> exclusions = new()
    {
        "Sailfish.TestAdapter.dll",
        "Tests.Sailfish.TestAdapter.dll"
    };

    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        sources = sources.ToList();
        logger.SendMessage(TestMessageLevel.Informational, $"Sources found in discoverer:\n{string.Join("\n-- ", sources)}");
        var filteredSource = sources.Where(x => !exclusions.Contains(x)).ToArray();

        if (filteredSource.Length == 0)
        {
            logger.SendMessage(TestMessageLevel.Warning, "No tests discovered.");
            return;
        }

        try
        {
            var testCases = TestDiscovery.DiscoverTests(filteredSource, logger).ToList();
            foreach (var testCase in testCases)
            {
                logger.SendMessage(TestMessageLevel.Informational, $"Sending TestCase: {testCase.DisplayName}");
                discoverySink.SendTestCase(testCase);
            }
        }
        catch (Exception ex)
        {
            throw new SailfishException(ex);
        }
    }
}