using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Analysis;
using Sailfish.Exceptions;
using Sailfish.TestAdapter.Discovery;
using Sailfish.TestAdapter.TestProperties;

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

        var sorted = SortTestCases(testCases, logger);

        foreach (var testCase in sorted)
        {
            discoverySink.SendTestCase(testCase);
        }
    }

    private static List<TestCase> SortTestCases(IEnumerable<TestCase> unsorted, IMessageLogger logger)
    {
        var classTestCases = unsorted
            .Select(
                testCase => new OrderClass(
                    testCase,
                    new TestCaseId(testCase.DisplayName).TestCaseVariables.Variables.Select(x => x.Value).ToArray()))
            .ToList();

        var firstCase = classTestCases.First();
        var testCaseId = new TestCaseId(firstCase.TestCase.DisplayName);
        var numVariables = testCaseId.TestCaseVariables.Variables.ToList().Count;

        var orderedCases = new List<TestCase>();
        try
        {
            switch (numVariables)
            {
                case 1:
                    var c1 = classTestCases
                        .OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty))
                        .ThenBy(x => x.Variables[0])
                        .Select(x => x.TestCase);
                    orderedCases.AddRange(c1);
                    break;
                case 2:
                    var c2 = classTestCases
                        .OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty))
                        .ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1])
                        .Select(x => x.TestCase);
                    orderedCases.AddRange(c2);
                    break;
                case 3:
                    var c3 = classTestCases
                        .OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty))
                        .ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1])
                        .ThenBy(x => x.Variables[2])
                        .Select(x => x.TestCase);
                    orderedCases.AddRange(c3);
                    break;
                case 4:
                    var c4 = classTestCases
                        .OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty))
                        .ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1])
                        .ThenBy(x => x.Variables[2])
                        .ThenBy(x => x.Variables[3])
                        .Select(x => x.TestCase);
                    orderedCases.AddRange(c4);
                    break;
                case 5:
                    var c5 = classTestCases.OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty))
                        .ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1])
                        .ThenBy(x => x.Variables[2])
                        .ThenBy(x => x.Variables[3])
                        .ThenBy(x => x.Variables[4])
                        .Select(x => x.TestCase);
                    orderedCases.AddRange(c5);
                    break;
                case 6:
                    var c6 = classTestCases.OrderBy(x => x.TestCase.GetPropertyValue(SailfishManagedProperty.SailfishMethodFilterProperty))
                        .ThenBy(x => x.Variables[0])
                        .ThenBy(x => x.Variables[1])
                        .ThenBy(x => x.Variables[2])
                        .ThenBy(x => x.Variables[3])
                        .ThenBy(x => x.Variables[4])
                        .ThenBy(x => x.Variables[5])
                        .Select(x => x.TestCase);
                    orderedCases.AddRange(c6);
                    break;
                default:
                    orderedCases.AddRange(classTestCases.Select(x => x.TestCase));
                    break;
            }
            logger.SendMessage(TestMessageLevel.Informational,"W DID IT!");
        }
        catch (Exception ex)
        {
            logger.SendMessage(TestMessageLevel.Error, ex.Message);
            logger.SendMessage(TestMessageLevel.Error, string.Join("\n", ex.StackTrace));
        }

        return orderedCases;
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