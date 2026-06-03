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
    private readonly IClassExecutionDispatcher _dispatcher;
    private readonly ILogger _logger;
    private readonly ITestCaseCountPrinter _testCaseCountPrinter;
    private readonly ITestInstanceContainerCreator _testInstanceContainerCreator;

    public SailFishTestExecutor(ILogger logger,
        ITestCaseCountPrinter testCaseCountPrinter,
        ITestInstanceContainerCreator testInstanceContainerCreator,
        IClassExecutionDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _testCaseCountPrinter = testCaseCountPrinter;
        _testInstanceContainerCreator = testInstanceContainerCreator;
    }

    public async Task<List<TestClassResultGroup>> Execute(
        IEnumerable<Type> testTypes,
        CancellationToken cancellationToken = default)
    {
        // Fresh tracker per executor run — claims do not leak across invocations.
        var lifecycleMethodTracker = new LifecycleMethodTracker();
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
                var testCaseExecutionResults = await Execute(testType, lifecycleMethodTracker, cancellationToken);
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
        LifecycleMethodTracker lifecycleMethodTracker,
        CancellationToken cancellationToken = default)
    {
        var testInstanceContainerProviders = _testInstanceContainerCreator.CreateTestContainerInstanceProviders(
            test, lifecycleMethodTracker: lifecycleMethodTracker);
        return await Execute(testInstanceContainerProviders, cancellationToken);
    }

    private async Task<List<TestCaseExecutionResult>> Execute(
        IReadOnlyCollection<TestInstanceContainerProvider> testInstanceContainerProviders,
        CancellationToken cancellationToken = default)
    {
        if (testInstanceContainerProviders.Count == 0) return new List<TestCaseExecutionResult>();

        foreach (var testProvider in testInstanceContainerProviders)
            _testCaseCountPrinter.PrintMethodUpdate(testProvider.Method);

        var testType = testInstanceContainerProviders.First().Test;
        return await _dispatcher.Dispatch(testType, testInstanceContainerProviders.ToList(), [], cancellationToken);
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