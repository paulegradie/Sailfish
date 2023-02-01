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
        var overallTotalCases = 0;
        var overallMethods = 0;
        foreach (var providers in listOfProviders)
        {
            var totalTestCases = 0;
            totalTestCases = providers.Aggregate(totalTestCases, (i, provider) => i + provider.GetNumberOfPropertySetsInTheQueue());
            overallTotalCases += totalTestCases;
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
        var testInstanceContainers = testInstanceContainerCreator.CreateTestContainerInstanceProviders(test);
        var results = await Execute(testInstanceContainers, callback, cancellationToken);
        return results;
    }

    private async Task<List<TestExecutionResult>> Execute(
        IReadOnlyCollection<TestInstanceContainerProvider> testMethods,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TestExecutionResult>();

        var methodIndex = 0;
        var totalMethodCount = testMethods.Count - 1;
        foreach (var testInstanceContainerProvider in testMethods.OrderBy(x => x.Method.Name))
        {
            TestCaseCountPrinter.PrintMethodUpdate(testInstanceContainerProvider.Method);
            var executionResults = await engine.ActivateContainer(methodIndex, totalMethodCount, testInstanceContainerProvider, callback, cancellationToken);
            results.AddRange(executionResults);
            methodIndex += 1;
        }

        return results;
    }

    private static bool FilterEnabledType(IEnumerable<Type> testTypes, out Type[] enabledTypes)
    {
        enabledTypes = testTypes.Where(x => !x.SailfishTypeIsDisabled()).ToArray();
        return enabledTypes.Length > 0;
    }
}