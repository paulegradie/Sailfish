using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.ExtensionMethods;
using Sailfish.Program;
using Serilog;

namespace Sailfish.Execution;

internal class SailFishTestExecutor : ISailFishTestExecutor
{
    private readonly ILogger logger;
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator;
    private readonly ISailfishExecutionEngine engine;

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
        Action<TestExecutionResult>? callback = null,
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
                var rawResult = await Execute(testType, callback, cancellationToken);
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

    private void SetConsoleTotals(IEnumerable<Type> enabledTestTypes)
    {
        var listOfProviders = enabledTestTypes.Select(x => testInstanceContainerCreator.CreateTestContainerInstanceProviders(x)).ToList();
        var overallTotalCases = 1;
        var overallMethods = 0;
        foreach (var providers in listOfProviders)
        {
            overallTotalCases += providers.Sum(provider => provider.GetNumberOfPropertySetsInTheQueue());
            overallMethods += providers.Select(x => x.Method).ToList().Count;
        }

        TestCaseCountPrinter.SetTestCaseTotal(overallTotalCases);
        TestCaseCountPrinter.SetTestMethodTotal(overallMethods);
    }

    private async Task<List<TestExecutionResult>> Execute(
        Type test,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default)
    {
        var testInstanceContainerProviders = testInstanceContainerCreator.CreateTestContainerInstanceProviders(test);
        var results = await Execute(testInstanceContainerProviders, callback, cancellationToken);
        return results;
    }

    private async Task<List<TestExecutionResult>> Execute(
        IReadOnlyCollection<TestInstanceContainerProvider> testInstanceContainerProviders,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TestExecutionResult>();

        var currentTestInstanceContainer = 1;
        var totalMethodCount = testInstanceContainerProviders.Count;
        foreach (var testInstanceContainerProvider in testInstanceContainerProviders.OrderBy(x => x.Method.Name))
        {
            TestCaseCountPrinter.PrintMethodUpdate(testInstanceContainerProvider.Method);
            var executionResults = await engine.ActivateContainer(currentTestInstanceContainer, totalMethodCount, testInstanceContainerProvider, callback, cancellationToken);
            results.AddRange(executionResults);
            currentTestInstanceContainer += 1;
        }

        return results;
    }

    private static bool FilterEnabledType(IEnumerable<Type> testTypes, out Type[] enabledTypes)
    {
        enabledTypes = testTypes.Where(x => !x.SailfishTypeIsDisabled()).ToArray();
        return enabledTypes.Length > 0;
    }
}