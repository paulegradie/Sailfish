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
public class TestDiscoverer : ITestDiscoverer
{
    private readonly ITestDiscovery discovery;

    private readonly List<string> exclusions =
    [
        "Sailfish.TestAdapter.dll",
        "Tests.Library.TestAdapter.dll"
    ];

    public TestDiscoverer()
    {
        discovery = new TestDiscovery();
    }

    public TestDiscoverer(ITestDiscovery discovery)
    {
        this.discovery = discovery;
    }

    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        sources = sources.ToList();
        var filteredSource = sources.Where(x => !exclusions.Contains(x)).Where(x => x.EndsWith(".dll")).ToArray();

        if (filteredSource.Length == 0)
        {
            logger.SendMessage(TestMessageLevel.Warning, "No tests discovered.");
            return;
        }

        var testCases = new List<TestCase>();
        try
        {
            var discoveredCases = discovery.DiscoverTests(filteredSource, logger).ToList();
            testCases.AddRange(discoveredCases);
        }
        catch (Exception ex)
        {
            logger.SendMessage(TestMessageLevel.Error, "Exception encountered in the Sailfish TestDiscoverer. :( ");
            logger.SendMessage(TestMessageLevel.Error, ex.Message);
            logger.SendMessage(TestMessageLevel.Error, string.Join("\n", ex.StackTrace));
            throw new SailfishException(ex);
        }

        foreach (var testCase in testCases) discoverySink.SendTestCase(testCase);
    }
}