using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;
using Sailfish.Logging;

namespace Sailfish.Execution;

internal interface ISailFishTestExecutor
{
    Task<List<TestClassResultGroup>> Execute(
        IEnumerable<Type> testTypes,
        CancellationToken cancellationToken = default);
}

internal class SailFishTestExecutor(
    ILogger logger,
    ITestCaseCountPrinter testCaseCountPrinter,
    ITestInstanceContainerCreator testInstanceContainerCreator,
    ISailfishExecutionEngine engine,
    IExecutionState executionState) : ISailFishTestExecutor
{
    private const string MemoryCacheName = "GlobalStateMemoryCache";
    private readonly ISailfishExecutionEngine engine = engine;
    private readonly ILogger logger = logger;
    private readonly ITestCaseCountPrinter testCaseCountPrinter = testCaseCountPrinter;
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator = testInstanceContainerCreator;

    public async Task<List<TestClassResultGroup>> Execute(
        IEnumerable<Type> testTypes,
        CancellationToken cancellationToken = default)
    {
        var allTestCaseResults = new List<TestClassResultGroup>();
        if (!FilterEnabledType(testTypes, out var enabledTestTypes))
        {
            logger.Log(LogLevel.Warning, "No Sailfish tests were discovered...");
            return allTestCaseResults;
        }

        SetConsoleTotals(enabledTestTypes);
        testCaseCountPrinter.SetTestTypeTotal(enabledTestTypes.Length);
        testCaseCountPrinter.PrintDiscoveredTotal();
        foreach (var testType in enabledTestTypes)
        {
            testCaseCountPrinter.PrintTypeUpdate(testType.Name);
            try
            {
                var testCaseExecutionResults = await Execute(testType, cancellationToken);
                allTestCaseResults.Add(new TestClassResultGroup(testType, testCaseExecutionResults));
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex, "The Test runner encountered an error - aborting ${testType.Full} test execution");
                allTestCaseResults.Add(new TestClassResultGroup(testType, new List<TestCaseExecutionResult>()));
            }
        }

        return allTestCaseResults;
    }

    private async Task<List<TestCaseExecutionResult>> Execute(
        Type test,
        CancellationToken cancellationToken = default)
    {
        var testInstanceContainerProviders = testInstanceContainerCreator.CreateTestContainerInstanceProviders(test);
        return await Execute(testInstanceContainerProviders, cancellationToken);
    }

    private async Task<List<TestCaseExecutionResult>> Execute(
        IReadOnlyCollection<TestInstanceContainerProvider> testInstanceContainerProviders,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TestCaseExecutionResult>();

        var testProviderIndex = 0;
        var totalMethodCount = testInstanceContainerProviders.Count - 1;
        foreach (var testProvider in testInstanceContainerProviders)
        {
            var providerPropertiesCacheKey = testProvider.Test.FullName ?? throw new SailfishException($"Failed to read the FullName of {testProvider.Test.Name}");
            testCaseCountPrinter.PrintMethodUpdate(testProvider.Method);
            var executionResults = await engine.ActivateContainer(
                testProviderIndex,
                totalMethodCount,
                testProvider,
                executionState,
                providerPropertiesCacheKey,
                cancellationToken);
            results.AddRange(executionResults);
            testProviderIndex += 1;
        }

        return results;
    }

    private static bool FilterEnabledType(IEnumerable<Type> testTypes, out Type[] enabledTypes)
    {
        enabledTypes = testTypes.Where(x => !x.SailfishTypeIsDisabled()).ToArray();
        return enabledTypes.Length > 0;
    }

    private void SetConsoleTotals(IEnumerable<Type> enabledTestTypes)
    {
        var listOfProviders = enabledTestTypes.Select(x => testInstanceContainerCreator.CreateTestContainerInstanceProviders(x)).ToList();
        var overallTotalCases = 0;
        var overallMethods = 0;
        foreach (var providers in listOfProviders)
        {
            overallTotalCases += providers.Sum(provider => Math.Max(provider.GetNumberOfPropertySetsInTheQueue(), 1));
            overallMethods += providers.Select(x => x.Method).ToList().Count;
        }

        testCaseCountPrinter.SetTestCaseTotal(overallTotalCases);
        testCaseCountPrinter.SetTestMethodTotal(overallMethods);
    }
}