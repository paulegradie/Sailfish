using System;
using System.Collections.Generic;
using System.Linq;
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

internal class SailFishTestExecutor : ISailFishTestExecutor
{
    private readonly ISailfishExecutionEngine _engine;
    private readonly ILogger _logger;
    private readonly ITestCaseCountPrinter _testCaseCountPrinter;
    private readonly ITestInstanceContainerCreator _testInstanceContainerCreator;

    public SailFishTestExecutor(ILogger logger,
        ITestCaseCountPrinter testCaseCountPrinter,
        ITestInstanceContainerCreator testInstanceContainerCreator,
        ISailfishExecutionEngine engine)
    {
        this._engine = engine;
        this._logger = logger;
        this._testCaseCountPrinter = testCaseCountPrinter;
        this._testInstanceContainerCreator = testInstanceContainerCreator;
    }

    public async Task<List<TestClassResultGroup>> Execute(
        IEnumerable<Type> testTypes,
        CancellationToken cancellationToken = default)
    {
        var allTestCaseResults = new List<TestClassResultGroup>();
        if (!FilterEnabledType(testTypes, out var enabledTestTypes))
        {
            _logger.Log(LogLevel.Warning, "No Sailfish tests were discovered...");
            return allTestCaseResults;
        }

        SetConsoleTotals(enabledTestTypes);
        _testCaseCountPrinter.SetTestTypeTotal(enabledTestTypes.Length);
        _testCaseCountPrinter.PrintDiscoveredTotal();
        foreach (var testType in enabledTestTypes)
        {
            _testCaseCountPrinter.PrintTypeUpdate(testType.Name);
            try
            {
                var testCaseExecutionResults = await Execute(testType, cancellationToken);
                allTestCaseResults.Add(new TestClassResultGroup(testType, testCaseExecutionResults));
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex, "The Test runner encountered an error - aborting ${testType.Full} test execution");
                allTestCaseResults.Add(new TestClassResultGroup(testType, new List<TestCaseExecutionResult>()));
            }
        }

        return allTestCaseResults;
    }

    private async Task<List<TestCaseExecutionResult>> Execute(
        Type test,
        CancellationToken cancellationToken = default)
    {
        var testInstanceContainerProviders = _testInstanceContainerCreator.CreateTestContainerInstanceProviders(test);
        return await Execute(testInstanceContainerProviders, cancellationToken);
    }

    private async Task<List<TestCaseExecutionResult>> Execute(
        IReadOnlyCollection<TestInstanceContainerProvider> testInstanceContainerProviders,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TestCaseExecutionResult>();

        var testProviderIndex = 0;
        var totalMethodCount = testInstanceContainerProviders.Count - 1;
        var executionState = new ExecutionState();
        foreach (var testProvider in testInstanceContainerProviders)
        {
            var providerPropertiesCacheKey = testProvider.Test.FullName ?? throw new SailfishException($"Failed to read the FullName of {testProvider.Test.Name}");
            _testCaseCountPrinter.PrintMethodUpdate(testProvider.Method);
            var executionResults = await _engine.ActivateContainer(
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
        var listOfProviders = enabledTestTypes.Select(x => _testInstanceContainerCreator.CreateTestContainerInstanceProviders(x)).ToList();
        var overallTotalCases = 0;
        var overallMethods = 0;
        foreach (var providers in listOfProviders)
        {
            overallTotalCases += providers.Sum(provider => Math.Max(provider.GetNumberOfPropertySetsInTheQueue(), 1));
            overallMethods += providers.Select(x => x.Method).ToList().Count;
        }

        _testCaseCountPrinter.SetTestCaseTotal(overallTotalCases);
        _testCaseCountPrinter.SetTestMethodTotal(overallMethods);
    }
}