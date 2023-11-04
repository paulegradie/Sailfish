using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediatR;
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
    Action<TestCaseExecutionResult, TestInstanceContainer> PostBenchmarkResultCallback(
        IEnumerable<TestCase> testCaseGroups,
        TrackingFileDataList preloadedLastRunIfAvailable,
        CancellationToken cancellationToken);

    Action<TestInstanceContainer?> BenchmarkDisabledCallback(IEnumerable<TestCase>? testCaseGroup);
    Action<TestInstanceContainer?> BenchmarkExceptionCallback(IEnumerable<TestCase> testCaseGroup);
    Action<TestInstanceContainer> PreBenchmarkResultCallback(IEnumerable<TestCase> testCaseGroup);
}

internal class AdapterActivatorCallbacks : IActivatorCallbacks
{
    private readonly IMediator mediator;
    private readonly IAdapterConsoleWriter consoleWriter;
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler;
    private readonly IAdapterSailDiff sailDiff;

    public AdapterActivatorCallbacks(IMediator mediator, IAdapterConsoleWriter consoleWriter, IClassExecutionSummaryCompiler classExecutionSummaryCompiler,
        IAdapterSailDiff sailDiff)
    {
        this.mediator = mediator;
        this.consoleWriter = consoleWriter;
        this.classExecutionSummaryCompiler = classExecutionSummaryCompiler;
        this.sailDiff = sailDiff;
    }

    public Action<TestCaseExecutionResult, TestInstanceContainer> PostBenchmarkResultCallback(
        IEnumerable<TestCase> testCaseGroups,
        TrackingFileDataList preloadedLastRunIfAvailable,
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
                    new TestClassResultGroup(result.TestInstanceContainer.Type, new List<TestCaseExecutionResult> { result }),
                    preloadedLastRunIfAvailable,
                    cancellationToken);
            }
            else
            {
                HandleFailureTestCase(
                    result,
                    currentTestCase,
                    new TestClassResultGroup(result.TestInstanceContainer.Type, new List<TestCaseExecutionResult> { result }),
                    cancellationToken);
            }
        };
    }

    private void HandleSuccessfulTestCase(
        TestCaseExecutionResult result,
        TestCase currentTestCase,
        TestClassResultGroup classResultGroup,
        IReadOnlyCollection<IReadOnlyCollection<IClassExecutionSummary>> preloadedLastRunIfAvailable,
        CancellationToken cancellationToken)
    {
        var classExecutionSummary = classExecutionSummaryCompiler
            .CompileToSummaries(new List<TestClassResultGroup>() { classResultGroup })
            .Single();
        var medianTestRuntime = classExecutionSummary.CompiledTestCaseResults.Single().PerformanceRunResult?.Median ??
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

        var formattedExecutionSummary = consoleWriter.Present(new[] { classExecutionSummary }, new OrderedDictionary());

        if (preloadedLastRunIfAvailable.Count > 0)
        {
            // preloadedLastRun represents an entire tracking file
            foreach (var preloadedLastRun in preloadedLastRunIfAvailable)
            {
                // iterate until we find a match, then break; its possible - when running a group of tests then a single test - for a tracking file to be created without expected data.
                var preloadedSummaryMatchingCurrentSummary = preloadedLastRun
                    .SelectMany(x => x.CompiledTestCaseResults)
                    .SingleOrDefault(x => x.TestCaseId?.DisplayName == result.TestInstanceContainer?.TestCaseId.DisplayName);
                if (preloadedSummaryMatchingCurrentSummary?.PerformanceRunResult is null) continue;

                // if we eventually find a previous run (we don't discriminate by age of run -- perhaps we should
                var testCaseResults = sailDiff.ComputeTestCaseDiff(
                    result,
                    classExecutionSummary,
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

        consoleWriter.RecordEnd(currentTestCase, testResult.Outcome);
        consoleWriter.RecordResult(testResult);
    }

    private void HandleFailureTestCase(
        TestCaseExecutionResult result,
        TestCase currentTestCase,
        TestClassResultGroup classResultGroup,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
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

        foreach (var exception in classResultGroup.ExecutionResults.Select(x => x.Exception).Where(x => x is not null).Cast<Exception>())
        {
            consoleWriter.WriteString("----- Exception -----", TestMessageLevel.Error);
            consoleWriter.WriteString(exception.Message, TestMessageLevel.Error);
        }

        consoleWriter.RecordEnd(currentTestCase, testResult.Outcome);
        consoleWriter.RecordResult(testResult);
    }

    public Action<TestInstanceContainer?> BenchmarkDisabledCallback(IEnumerable<TestCase>? testCaseGroup)
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

    public Action<TestInstanceContainer?> BenchmarkExceptionCallback(IEnumerable<TestCase> testCaseGroup)
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

    public Action<TestInstanceContainer> PreBenchmarkResultCallback(IEnumerable<TestCase> testCaseGroup)
    {
        return container => consoleWriter.RecordStart(GetTestCaseFromTestCaseGroupMatchingCurrentContainer(container, testCaseGroup));
    }

    private static TestCase GetTestCaseFromTestCaseGroupMatchingCurrentContainer(TestInstanceContainer container, IEnumerable<TestCase> testCaseGroup)
    {
        return testCaseGroup.Single(testCase =>
            container.TestCaseId.DisplayName.EndsWith(testCase.GetPropertyHelper(SailfishManagedProperty.SailfishDisplayNameDefinitionProperty)));
    }
}