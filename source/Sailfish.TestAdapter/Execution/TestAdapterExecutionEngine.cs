using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Analysis.SailDiff;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterExecutionEngine : ITestAdapterExecutionEngine
{
    private const string MemoryCacheName = "GlobalStateMemoryCache";
    private readonly IActivatorCallbacks activatorCallbacks;
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator;
    private readonly IExecutionSummaryCompiler executionSummaryCompiler;
    private readonly ISailfishExecutionEngine engine;
    private readonly IAdapterConsoleWriter consoleWriter;

    public TestAdapterExecutionEngine(
        IActivatorCallbacks activatorCallbacks,
        ITestInstanceContainerCreator testInstanceContainerCreator,
        IExecutionSummaryCompiler executionSummaryCompiler,
        ISailfishExecutionEngine engine,
        IAdapterConsoleWriter consoleWriter
    )
    {
        this.activatorCallbacks = activatorCallbacks;
        this.testInstanceContainerCreator = testInstanceContainerCreator;
        this.executionSummaryCompiler = executionSummaryCompiler;
        this.engine = engine;
        this.consoleWriter = consoleWriter;
    }

    public async Task<List<IExecutionSummary>> Execute(
        List<TestCase> testCases,
        List<List<IExecutionSummary>> preloadedLastRunIfAvailable,
        TestSettings? testSettings,
        CancellationToken cancellationToken)
    {
        var rawExecutionResults = new List<(string, RawExecutionResult)>();
        var testCaseGroups = testCases.GroupBy(testCase => testCase.GetPropertyHelper(SailfishManagedProperty.SailfishTypeProperty));

        foreach (var unsortedTestCaseGroup in testCaseGroups)
        {
            var groupResults = new List<TestExecutionResult>();

            var groupKey = unsortedTestCaseGroup.Key;

            if (GetTypeTypeForGroup(unsortedTestCaseGroup, out var testType)) continue;
            if (testType is null) continue;

            // list of methods with their many variable combos. Each element is a container, which represents a SailfishMethod
            var providerForCurrentTestCases =
                testInstanceContainerCreator
                    .CreateTestContainerInstanceProviders(
                        testType,
                        CreatePropertyFilter(GetTestCaseProperties(SailfishManagedProperty.SailfishFormedVariableSectionDefinitionProperty, testCases)),
                        CreateMethodFilter(GetTestCaseProperties(SailfishManagedProperty.SailfishMethodFilterProperty, testCases)));

            var totalTestProviderCount = providerForCurrentTestCases.Count - 1;
            var memoryCache = new MemoryCache(MemoryCacheName);
            for (var i = 0; i < providerForCurrentTestCases.Count; i++)
            {
                var testProvider = providerForCurrentTestCases[i];
                var providerPropertiesCacheKey = testProvider.Test.FullName ?? throw new SailfishException($"Failed to read the FullName of {testProvider.Test.Name}");
                var results = await engine.ActivateContainer(
                    i,
                    totalTestProviderCount,
                    testProvider,
                    memoryCache,
                    providerPropertiesCacheKey,
                    activatorCallbacks.PreTestResultCallback(unsortedTestCaseGroup),
                    activatorCallbacks.PostTestResultCallback(unsortedTestCaseGroup, preloadedLastRunIfAvailable, testSettings, cancellationToken),
                    activatorCallbacks.ExceptionCallback(unsortedTestCaseGroup),
                    activatorCallbacks.TestDisabledCallback(unsortedTestCaseGroup),
                    cancellationToken);
                groupResults.AddRange(results);
            }

            rawExecutionResults.Add((groupKey, new RawExecutionResult(testType, groupResults)));
        }

        var executionSummaries = executionSummaryCompiler
            .CompileToSummaries(rawExecutionResults.Select(x => x.Item2), cancellationToken)
            .ToList();

        return executionSummaries;
    }

    private static IEnumerable<string> GetTestCaseProperties(TestProperty testProperty, IEnumerable<TestCase> testCases)
    {
        return testCases.Select(x => x.GetPropertyHelper(testProperty)).Distinct();
    }

    private bool GetTypeTypeForGroup(IEnumerable<TestCase> unsortedTestCaseGroup, out Type? testType)
    {
        var aTestCase = unsortedTestCaseGroup.First();
        var testTypeFullName = aTestCase.GetPropertyHelper(SailfishManagedProperty.SailfishTypeProperty);
        var assembly = LoadAssemblyFromDll(aTestCase.Source);
        testType = assembly.GetType(testTypeFullName, true, true);
        if (testType is not null) return false;
        consoleWriter.WriteString($"Unable to find the following testType: {testTypeFullName}");
        return true;
    }

    private static Assembly LoadAssemblyFromDll(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        return assembly;
    }

    private static Func<MethodInfo, bool> CreateMethodFilter(IEnumerable<string> availableMethods)
    {
        return (MethodInfo currentMethodInfo) =>
        {
            var currentMethod = currentMethodInfo.Name;
            return availableMethods.Contains(currentMethod);
        };
    }

    private static Func<PropertySet, bool> CreatePropertyFilter(IEnumerable<string> availableVariableSections)
    {
        return (PropertySet currentPropertySet) =>
        {
            var currentVariableSection = currentPropertySet.FormTestCaseVariableSection();
            return availableVariableSections.Contains(currentVariableSection);
        };
    }
}