using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.ExtensionMethods;
using Serilog;

namespace Sailfish.Execution;

internal class SailFishTestExecutor : ISailFishTestExecutor
{
    private readonly ILogger logger;
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator;
    private readonly ISailfishExecutionEngine engine;

    public SailFishTestExecutor(ILogger logger, ITestInstanceContainerCreator testInstanceContainerCreator, ISailfishExecutionEngine engine)
    {
        this.logger = logger;
        this.testInstanceContainerCreator = testInstanceContainerCreator;
        this.engine = engine;
    }

    private static bool FilterEnabledType(IEnumerable<Type> testTypes, out Type[] enabledTypes)
    {
        enabledTypes = testTypes.Where(x => !x.SailfishTypeIsDisabled()).ToArray();
        return enabledTypes.Length > 0;
    }

    public async Task<List<RawExecutionResult>> Execute(IEnumerable<Type> testTypes, Action<TestExecutionResult>? callback = null, CancellationToken cancellationToken = default)
    {
        var rawResults = new List<RawExecutionResult>();
        if (!FilterEnabledType(testTypes, out var enabledTestTypes))
        {
            logger.Warning("No Sailfish tests were discovered...");
            return rawResults;
        }

        var testIndex = 0;
        var totalTestCount = enabledTestTypes.Length;

        logger.Information("Discovered {TotalTestCount} enabled test types", totalTestCount);
        foreach (var testType in enabledTestTypes)
        {
            try
            {
                logger.Information("Executing test type {TestIndex} of {TotalTestCount}: {TestName}", testIndex + 1, totalTestCount, testType.Name);
                var rawResult = await Execute(testType, callback, cancellationToken);
                rawResults.Add(new RawExecutionResult(testType, rawResult));
            }
            catch (Exception ex)
            {
                logger.Fatal("The Test runner encountered a fatal error: {Message}", ex.Message);
                rawResults.Add(new RawExecutionResult(testType, ex));
            }

            testIndex += 1;
        }

        return rawResults;
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
            logger.Information(
                "Executing test method {MethodIndex} of {TotalMethodCount}: {TestTypeName}.{TestMethodName}",
                (methodIndex + 1).ToString(), (totalMethodCount + 1).ToString(), testInstanceContainerProvider.Method.DeclaringType?.Name, testInstanceContainerProvider.Method.Name);
            var executionResults = await engine.ActivateContainer(methodIndex, totalMethodCount, testInstanceContainerProvider, callback, cancellationToken);
            results.AddRange(executionResults);
            methodIndex += 1;
        }

        return results;
    }
}