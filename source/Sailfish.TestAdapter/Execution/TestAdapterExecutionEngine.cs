using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Analysis.Saildiff;
using Sailfish.Contracts.Public;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Execution;

internal class TestAdapterExecutionEngine : ITestAdapterExecutionEngine
{
    private readonly IAdapterSailDiff sailDiff;
    private readonly ITestInstanceContainerCreator testInstanceContainerCreator;
    private readonly IExecutionSummaryCompiler executionSummaryCompiler;
    private readonly ISailfishExecutionEngine engine;
    private readonly IAdapterConsoleWriter consoleWriter;
    private const string MemoryCacheName = "GlobalStateMemoryCache";

    public TestAdapterExecutionEngine(
        IAdapterSailDiff sailDiff,
        ITestInstanceContainerCreator testInstanceContainerCreator,
        IExecutionSummaryCompiler executionSummaryCompiler,
        ISailfishExecutionEngine engine,
        IAdapterConsoleWriter consoleWriter
    )
    {
        this.sailDiff = sailDiff;
        this.testInstanceContainerCreator = testInstanceContainerCreator;
        this.executionSummaryCompiler = executionSummaryCompiler;
        this.engine = engine;
        this.consoleWriter = consoleWriter;
    }

    public List<IExecutionSummary> Execute(
        List<TestCase> testCases,
        List<DescriptiveStatisticsResult> preloadedLastRunIfAvailable,
        TestSettings? testSettings,
        CancellationToken cancellationToken)
    {
        var rawExecutionResults = new List<(string, RawExecutionResult)>();
        var testCaseGroups = testCases.GroupBy(testCase => testCase.GetPropertyHelper(SailfishManagedProperty.SailfishTypeProperty));

        foreach (var testCaseGroup in testCaseGroups)
        {
            var groupResults = new List<TestExecutionResult>();

            var firstTestCase = testCaseGroup.First();
            var testTypeFullName = firstTestCase.GetPropertyHelper(SailfishManagedProperty.SailfishTypeProperty);
            var assembly = LoadAssemblyFromDll(firstTestCase.Source);
            var testType = assembly.GetType(testTypeFullName, true, true);
            if (testType is null)
            {
                consoleWriter.WriteString($"Unable to find the following testType: {testTypeFullName}");
                continue;
            }

            var availableVariableSections = testCases
                .Select(x => x.GetPropertyHelper(SailfishManagedProperty.SailfishFormedVariableSectionDefinitionProperty))
                .Distinct();

            bool PropertyFilter(PropertySet currentPropertySet)
            {
                var currentVariableSection = currentPropertySet.FormTestCaseVariableSection();
                return availableVariableSections.Contains(currentVariableSection);
            }

            var availableMethods = testCases
                .Select(x => x.GetPropertyHelper(SailfishManagedProperty.SailfishMethodFilterProperty))
                .Distinct();

            bool MethodFilter(MethodInfo currentMethodInfo)
            {
                var currentMethod = currentMethodInfo.Name;
                return availableMethods.Contains(currentMethod);
            }

            // list of methods with their many variable combos. Each element is a container, which represents a SailfishMethod
            var providerForCurrentTestCases =
                testInstanceContainerCreator
                    .CreateTestContainerInstanceProviders(
                        testType,
                        PropertyFilter,
                        MethodFilter);

            var totalTestProviderCount = providerForCurrentTestCases.Count - 1;
            var memoryCache = new MemoryCache(MemoryCacheName);
            for (var i = 0; i < providerForCurrentTestCases.Count; i++)
            {
                var testProvider = providerForCurrentTestCases[i];
                var providerPropertiesCacheKey = testProvider.Test.FullName ?? throw new SailfishException($"Failed to read the FullName of {testProvider.Test.Name}");
                var results = engine.ActivateContainer(
                        i,
                        totalTestProviderCount,
                        testProvider,
                        memoryCache,
                        providerPropertiesCacheKey,
                        PreTestResultCallback(testCaseGroup),
                        PostTestResultCallback(testCaseGroup, preloadedLastRunIfAvailable, testSettings, cancellationToken),
                        ExceptionCallback(testCaseGroup),
                        TestDisabledCallback(testCaseGroup),
                        cancellationToken)
                    .GetAwaiter().GetResult();
                groupResults.AddRange(results);
            }

            rawExecutionResults.Add((testCaseGroup.Key, new RawExecutionResult(testType, groupResults)));
        }

        var executionSummaries = executionSummaryCompiler
            .CompileToSummaries(rawExecutionResults.Select(x => x.Item2), cancellationToken)
            .ToList();

        return executionSummaries;
    }


    private Action<TestExecutionResult, TestInstanceContainer> PostTestResultCallback(
        IGrouping<string, TestCase> testCaseGroups,
        List<DescriptiveStatisticsResult> preloadedLastRunIfAvailable,
        TestSettings? testSettings,
        CancellationToken cancellationToken)
    {
        return (result, container) =>
        {
            if (result.PerformanceTimerResults is null)
            {
                var msg = $"PerformanceTimerResults was null for {container.Type.Name}";
                consoleWriter.WriteString(msg, TestMessageLevel.Error);
                throw new SailfishException(msg);
            }

            if (result.TestInstanceContainer is null)
            {
                var msg = $"TestInstanceContainer was null for {container.Type.Name}";
                consoleWriter.WriteString(msg, TestMessageLevel.Error);
                throw new SailfishException(msg);
            }

            var currentTestCase = GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroups);
            if (result.IsSuccess)
            {
                HandleSuccessfulTestCase(
                    result,
                    currentTestCase,
                    new RawExecutionResult(result.TestInstanceContainer.Type, new List<TestExecutionResult> { result }),
                    preloadedLastRunIfAvailable,
                    testSettings,
                    cancellationToken);
            }
            else
            {
                HandleFailureTestCase(
                    result,
                    currentTestCase,
                    new RawExecutionResult(result.TestInstanceContainer.Type,
                        result.Exception ??
                        new Exception($"The exception details were null for {result.TestInstanceContainer.Type.Name}")),
                    cancellationToken);
            }
        };
    }

    private void HandleSuccessfulTestCase(
        TestExecutionResult result,
        TestCase currentTestCase,
        RawExecutionResult rawResult,
        IReadOnlyCollection<DescriptiveStatisticsResult> preloadedLastRunIfAvailable,
        TestSettings? testSettings,
        CancellationToken cancellationToken)
    {
        var executionSummary = executionSummaryCompiler
            .CompileToSummaries(new List<RawExecutionResult>() { rawResult }, cancellationToken)
            .Single();
        var medianTestRuntime = executionSummary.CompiledTestCaseResults.Single().DescriptiveStatisticsResult?.Median ??
                                throw new SailfishException("Error computing compiled results");

        var testResult = new TestResult(currentTestCase);

        if (result.Exception is not null)
        {
            testResult.ErrorMessage = result.Exception.Message;
            testResult.ErrorStackTrace = result.Exception.StackTrace;
        }

        testResult.Outcome = result.StatusCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
        testResult.DisplayName = currentTestCase.DisplayName;

        testResult.StartTime = result.PerformanceTimerResults?.GlobalStart ?? new DateTimeOffset();
        testResult.EndTime = result.PerformanceTimerResults?.GlobalStop ?? new DateTimeOffset();
        testResult.Duration = TimeSpan.FromMilliseconds(double.IsNaN(medianTestRuntime) ? 0 : medianTestRuntime);

        testResult.ErrorMessage = result.Exception?.Message;

        var formattedExecutionSummary = consoleWriter.Present(new[] { executionSummary }, new OrderedDictionary());

        if (preloadedLastRunIfAvailable.Count > 0 && testSettings is not null)
        {
            var testCaseResults = sailDiff.ComputeTestCaseDiff(result, executionSummary, testSettings, preloadedLastRunIfAvailable, cancellationToken);
            formattedExecutionSummary += "\n" + testCaseResults;
        }

        testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, formattedExecutionSummary));

        if (result.Exception is not null)
        {
            testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, result.Exception?.Message));
        }

        LogTestResults(result);

        consoleWriter.RecordEnd(currentTestCase, testResult.Outcome);
        consoleWriter.RecordResult(testResult);
    }

    private void HandleFailureTestCase(
        TestExecutionResult result,
        TestCase currentTestCase,
        RawExecutionResult rawResult,
        CancellationToken cancellationToken)
    {
        var testResult = new TestResult(currentTestCase);

        if (result.Exception is not null)
        {
            testResult.ErrorMessage = result.Exception.Message;
            testResult.ErrorStackTrace = result.Exception.StackTrace;
        }

        testResult.Outcome = result.StatusCode == 0 ? TestOutcome.Passed : TestOutcome.Failed;
        testResult.DisplayName = currentTestCase.DisplayName;

        testResult.StartTime = result.PerformanceTimerResults?.GlobalStart ?? new DateTimeOffset();
        testResult.EndTime = result.PerformanceTimerResults?.GlobalStop ?? new DateTimeOffset();
        testResult.Duration = TimeSpan.Zero;

        testResult.Messages.Clear();
        testResult.ErrorMessage = result.Exception?.Message;

        foreach (var exception in rawResult.Exceptions)
        {
            consoleWriter.WriteString("----- Exception -----", TestMessageLevel.Error);
            consoleWriter.WriteString(exception.Message, TestMessageLevel.Error);
        }

        consoleWriter.RecordEnd(currentTestCase, testResult.Outcome);
        consoleWriter.RecordResult(testResult);
    }


    private static Assembly LoadAssemblyFromDll(string dllPath)
    {
        var assembly = Assembly.LoadFile(dllPath);
        AppDomain.CurrentDomain.Load(assembly.GetName()); // is this necessary?
        return assembly;
    }

    private void LogTestResults(TestExecutionResult result)
    {
        foreach (var perf in result.PerformanceTimerResults?.MethodIterationPerformances!)
        {
            var timeResult = perf.GetDurationFromTicks().MilliSeconds;
            consoleWriter.WriteString($"Time: {timeResult.Duration.ToString(CultureInfo.InvariantCulture)} {timeResult.TimeScale.ToString().ToLowerInvariant()}");
        }
    }

    private Action<TestInstanceContainer?> TestDisabledCallback(IGrouping<string, TestCase>? testCaseGroup)
    {
        return container =>
        {
            if (container is null || testCaseGroup is null) return;

            var currentTestCase = testCaseGroup.Single(x => x.DisplayName == container.TestCaseId.GetMethodWithVariables());
            var testResult = new TestResult(currentTestCase)
            {
                ErrorMessage = $"Test Disabled",
                ErrorStackTrace = null,
                Outcome = TestOutcome.Skipped,
                DisplayName = currentTestCase.DisplayName,
                ComputerName = null,
                Duration = TimeSpan.Zero,
                StartTime = default,
                EndTime = default
            };

            consoleWriter.RecordEnd(currentTestCase, testResult.Outcome);
            consoleWriter.RecordResult(testResult);
        };
    }

    private Action<TestInstanceContainer?> ExceptionCallback(IGrouping<string, TestCase> testCaseGroup)
    {
        return (container) =>
        {
            if (container is null)
            {
                foreach (var testCase in testCaseGroup)
                {
                    consoleWriter.RecordEnd(testCase, TestOutcome.Failed);
                }
            }
            else
            {
                var currentTestCase = GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroup);
                consoleWriter.RecordEnd(currentTestCase, TestOutcome.Failed);
            }
        };
    }

    private Action<TestInstanceContainer> PreTestResultCallback(IGrouping<string, TestCase> testCaseGroup)
    {
        return container => consoleWriter.RecordStart(GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroup));
    }

    private static TestCase GetTestCaseFromTestCaseGroupMatchingCurrentContainer(TestInstanceContainer container, IEnumerable<TestCase> testCaseGroup)
    {
        return testCaseGroup.Single(x => container.TestCaseId.DisplayName.EndsWith(x.DisplayName));
    }
}