using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.TestAdapter.TestProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Execution;

internal interface ITestAdapterExecutionEngine
{
    Task<List<IClassExecutionSummary>> Execute(
        List<TestCase> testCases,
        CancellationToken cancellationToken);
}

internal class TestAdapterExecutionEngine : ITestAdapterExecutionEngine
{
    private const string MemoryCacheName = "GlobalStateMemoryCache";
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler;
    private readonly IAdapterConsoleWriter consoleWriter;
    private readonly ISailfishExecutionEngine engine;
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator;

    public TestAdapterExecutionEngine(
        ITestInstanceContainerCreator testInstanceContainerCreator,
        IClassExecutionSummaryCompiler classExecutionSummaryCompiler,
        ISailfishExecutionEngine engine,
        IAdapterConsoleWriter consoleWriter
    )
    {
        this.testInstanceContainerCreator = testInstanceContainerCreator;
        this.classExecutionSummaryCompiler = classExecutionSummaryCompiler;
        this.engine = engine;
        this.consoleWriter = consoleWriter;
    }

    public async Task<List<IClassExecutionSummary>> Execute(List<TestCase> testCases, CancellationToken cancellationToken)
    {
        var rawExecutionResults = new List<(string, TestClassResultGroup)>();
        var testCaseGroups = testCases.GroupBy(testCase => testCase.GetPropertyHelper(SailfishManagedProperty.SailfishTypeProperty));

        foreach (var unsortedTestCaseGroup in testCaseGroups)
        {
            if (GetTypeTypeForGroup(unsortedTestCaseGroup, out var testType)) continue;
            if (testType is null) continue;

            // list of methods with their many variable combos. Each element is a container, which represents a SailfishMethod
            var providerForCurrentTestCases = testInstanceContainerCreator
                .CreateTestContainerInstanceProviders(
                    testType,
                    CreatePropertyFilter(GetTestCaseProperties(SailfishManagedProperty.SailfishFormedVariableSectionDefinitionProperty, testCases)),
                    CreateMethodFilter(GetTestCaseProperties(SailfishManagedProperty.SailfishMethodFilterProperty, testCases)));

            var totalTestProviderCount = providerForCurrentTestCases.Count - 1;

            // reset a memory cache to hold class property values when transferring them between instances
            var memoryCache = new MemoryCache(MemoryCacheName);

            var groupResults = new List<TestCaseExecutionResult>();
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
                    unsortedTestCaseGroup,
                    cancellationToken);
                groupResults.AddRange(results);

                // deref to free up memory
                providerForCurrentTestCases[i] = null!;
            }

            rawExecutionResults.Add((unsortedTestCaseGroup.Key, new TestClassResultGroup(testType, groupResults)));
        }

        return classExecutionSummaryCompiler.CompileToSummaries(rawExecutionResults.Select(x => x.Item2)).ToList();
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
        return currentMethodInfo => availableMethods.Contains(currentMethodInfo.Name);
    }

    private static Func<PropertySet, bool> CreatePropertyFilter(IEnumerable<string> availableVariableSections)
    {
        return currentPropertySet => availableVariableSections.Contains(currentPropertySet.FormTestCaseVariableSection());
    }
}