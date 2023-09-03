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
    private readonly List<string> exclusions = new()
    {
        "Sailfish.TestAdapter.dll",
        "Tests.Sailfish.TestAdapter.dll"
    };

    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        sources = sources.ToList();
        var filteredSource = sources.Where(x => !exclusions.Contains(x)).ToArray();

        if (filteredSource.Length == 0)
        {
            logger.SendMessage(TestMessageLevel.Warning, "No tests discovered.");
            return;
        }

        var testCases = new List<TestCase>();
        try
        {
            var discoveredCases = TestDiscovery.DiscoverTests(filteredSource, logger).ToList();
            testCases.AddRange(discoveredCases);
        }
        catch (Exception ex)
        {
            logger.SendMessage(TestMessageLevel.Error, "Exception encountered in the Sailfish TestDiscoverer. :( ");
            logger.SendMessage(TestMessageLevel.Error, ex.Message);
            logger.SendMessage(TestMessageLevel.Error, string.Join("\n", ex.StackTrace));
            throw new SailfishException(ex);
        }

        // var sorted = SortTestCases(testCases, logger);

        foreach (var testCase in testCases)
        {
            discoverySink.SendTestCase(testCase);
        }
    }

    private class OrderClass
    {
        public OrderClass(TestCase testCase, object[] variables)
        {
            TestCase = testCase;
            Variables = variables;
        }

        public TestCase TestCase { get; set; }
        public object[] Variables { get; set; }
    }
}