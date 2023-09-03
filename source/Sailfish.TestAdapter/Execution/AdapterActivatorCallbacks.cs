using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Analysis.SailDiff;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Execution;

internal interface IActivatorCallbacks
{
    Action<TestExecutionResult, TestInstanceContainer> PostTestResultCallback(
        IEnumerable<TestCase> testCaseGroups,
        List<List<IExecutionSummary>> preloadedLastRunIfAvailable,
        TestSettings? testSettings,
        CancellationToken cancellationToken);

    Action<TestInstanceContainer?> TestDisabledCallback(IEnumerable<TestCase>? testCaseGroup);
    Action<TestInstanceContainer?> ExceptionCallback(IEnumerable<TestCase> testCaseGroup);
    Action<TestInstanceContainer> PreTestResultCallback(IEnumerable<TestCase> testCaseGroup);
}

internal class AdapterActivatorCallbacks : IActivatorCallbacks
{
    private readonly IAdapterConsoleWriter consoleWriter;
    private readonly IExecutionSummaryCompiler executionSummaryCompiler;
    private readonly IAdapterSailDiff sailDiff;

    public AdapterActivatorCallbacks(IAdapterConsoleWriter consoleWriter, IExecutionSummaryCompiler executionSummaryCompiler, IAdapterSailDiff sailDiff)
    {
        this.consoleWriter = consoleWriter;
        this.executionSummaryCompiler = executionSummaryCompiler;
        this.sailDiff = sailDiff;
    }

    public Action<TestExecutionResult, TestInstanceContainer> PostTestResultCallback(
        IEnumerable<TestCase> testCaseGroups,
        List<List<IExecutionSummary>> preloadedLastRunIfAvailable,
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
        IReadOnlyCollection<IReadOnlyCollection<IExecutionSummary>> preloadedLastRunIfAvailable,
        TestSettings? testSettings,
        CancellationToken cancellationToken)
    {
        var executionSummary = executionSummaryCompiler
            .CompileToSummaries(new List<RawExecutionResult>() { rawResult }, cancellationToken)
            .Single();
        var medianTestRuntime = executionSummary.CompiledTestCaseResults.Single().PerformanceRunResult?.Median ??
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
            // preloadedLastRun represents an entire tracking file
            foreach (var preloadedLastRun in preloadedLastRunIfAvailable)
            {
                // iterate until we fine a match, then break; its possible - when running a group of tests then a single test - for a tracking file to be created without expected data.
                var preloadedSummaryMatchingCurrentSummary = preloadedLastRun
                    .SelectMany(x => x.CompiledTestCaseResults)
                    .SingleOrDefault(x => x.TestCaseId?.DisplayName == result.TestInstanceContainer?.TestCaseId.DisplayName);
                if (preloadedSummaryMatchingCurrentSummary?.PerformanceRunResult is null) continue;

                // if we eventually find a previous run (we don't discriminate by age of run -- perhaps we should
                var testCaseResults = sailDiff.ComputeTestCaseDiff(
                    result,
                    executionSummary,
                    testSettings,
                    preloadedSummaryMatchingCurrentSummary.PerformanceRunResult,
                    cancellationToken);
                formattedExecutionSummary += "\n" + testCaseResults;
                break;
            }
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

    public Action<TestInstanceContainer?> TestDisabledCallback(IEnumerable<TestCase>? testCaseGroup)
    {
        void CreateDisabledResult(TestCase testCase)
        {
            var testResult = new TestResult(testCase)
            {
                ErrorMessage = $"Test Disabled",
                ErrorStackTrace = null,
                Outcome = TestOutcome.Skipped,
                DisplayName = testCase.DisplayName,
                ComputerName = null,
                Duration = TimeSpan.Zero,
                StartTime = default,
                EndTime = default
            };

            consoleWriter.RecordEnd(testCase, testResult.Outcome);
            consoleWriter.RecordResult(testResult);
        }

        return container =>
        {
            if (testCaseGroup is null) return; // no idea why this would happen, but exceptions are not the way
            if (container is null) // then we've disabled the class - return all the results for the group
            {
                foreach (var testCase in testCaseGroup)
                {
                    CreateDisabledResult(testCase);
                }
            }
            else // we've only disabled this method, send a single result
            {
                var currentTestCase = GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroup);
                CreateDisabledResult(currentTestCase);
            }
        };
    }

    public Action<TestInstanceContainer?> ExceptionCallback(IEnumerable<TestCase> testCaseGroup)
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

    public Action<TestInstanceContainer> PreTestResultCallback(IEnumerable<TestCase> testCaseGroup)
    {
        return container => consoleWriter.RecordStart(GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroup));
    }

    private static TestCase GetTestCaseFromTestCaseGroupMatchingCurrentContainer(TestInstanceContainer container, IEnumerable<TestCase> testCaseGroup)
    {
        return testCaseGroup.Single(testCase =>
            container.TestCaseId.DisplayName.EndsWith(testCase.GetPropertyHelper(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty)));
    }

    private void LogTestResults(TestExecutionResult result)
    {
        foreach (var perf in result.PerformanceTimerResults?.MethodIterationPerformances!)
        {
            var timeResult = perf.GetDurationFromTicks().MilliSeconds;
            consoleWriter.WriteString($"Time: {timeResult.Duration.ToString(CultureInfo.InvariantCulture)} {timeResult.TimeScale.ToString().ToLowerInvariant()}");
        }
    }
}