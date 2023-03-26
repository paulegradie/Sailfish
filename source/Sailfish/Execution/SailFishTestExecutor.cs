using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;
using Sailfish.Program;
using Serilog;

namespace Sailfish.Execution;

internal class SailFishTestExecutor : ISailFishTestExecutor
{
    private readonly ILogger logger;
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator;
    private readonly ISailfishExecutionEngine engine;
    private const string MemoryCacheName = "GlobalStateMemoryCache";

    public SailFishTestExecutor(
        ILogger logger,
        ITestInstanceContainerCreator testInstanceContainerCreator,
        ISailfishExecutionEngine engine)
    {
        this.logger = logger;
        this.testInstanceContainerCreator = testInstanceContainerCreator;
        this.engine = engine;
    }

    public async Task<List<RawExecutionResult>> Execute(
        IEnumerable<Type> testTypes,
        CancellationToken cancellationToken = default)
    {
        var rawResults = new List<RawExecutionResult>();
        if (!FilterEnabledType(testTypes, out var enabledTestTypes))
        {
            logger.Warning("No Sailfish tests were discovered...");
            return rawResults;
        }

        SetConsoleTotals(enabledTestTypes);
        TestCaseCountPrinter.SetLogger(logger);
        TestCaseCountPrinter.SetTestTypeTotal(enabledTestTypes.Length);
        TestCaseCountPrinter.PrintDiscoveredTotal();
        foreach (var testType in enabledTestTypes)
        {
            TestCaseCountPrinter.PrintTypeUpdate(testType.Name);
            try
            {
                var rawResult = await Execute(testType, cancellationToken);
                rawResults.Add(new RawExecutionResult(testType, rawResult));
            }
            catch (Exception ex)
            {
                logger.Fatal("The Test runner encountered a fatal error: {Message}", ex.Message);
                rawResults.Add(new RawExecutionResult(testType, ex));
            }
        }

        return rawResults;
    }

    private async Task<List<TestExecutionResult>> Execute(
        Type test,
        CancellationToken cancellationToken = default)
    {
        var testInstanceContainerProviders = testInstanceContainerCreator.CreateTestContainerInstanceProviders(test);
        var results = await Execute(testInstanceContainerProviders, cancellationToken);
        return results;
    }

    private async Task<List<TestExecutionResult>> Execute(
        IReadOnlyCollection<TestInstanceContainerProvider> testInstanceContainerProviders,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TestExecutionResult>();

        var testProviderIndex = 0;
        var totalMethodCount = testInstanceContainerProviders.Count - 1;
        
        var memoryCache = new MemoryCache(MemoryCacheName);
        foreach (var testProvider in testInstanceContainerProviders.OrderBy(x => x.Method.Name))
        {
            var providerPropertiesCacheKey = testProvider.Test.FullName ?? throw new SailfishException($"Failed to read the FullName of {testProvider.Test.Name}");
            TestCaseCountPrinter.PrintMethodUpdate(testProvider.Method);
            var executionResults = await engine.ActivateContainer(
                testProviderIndex,
                totalMethodCount,
                testProvider,
                memoryCache,
                providerPropertiesCacheKey,
                cancellationToken: cancellationToken);
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

        TestCaseCountPrinter.SetTestCaseTotal(overallTotalCases);
        TestCaseCountPrinter.SetTestMethodTotal(overallMethods);
    }
}