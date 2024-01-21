using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Contracts.Private.ExecutionCallbackHandlers;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.TestAdapter.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.FrameworkHandlers;
internal class ExecutionCompletedNotificationHandler(IClassExecutionSummaryCompiler classExecutionSummaryCompiler, IAdapterConsoleWriter consoleWriter, IRunSettings runSettings, IMediator mediator, IAdapterSailDiff sailDiff) : INotificationHandler<ExecutionCompletedNotification>
{
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler = classExecutionSummaryCompiler;
    private readonly IAdapterConsoleWriter consoleWriter = consoleWriter;
    private readonly IRunSettings runSettings = runSettings;
    private readonly IMediator mediator = mediator;
    private readonly IAdapterSailDiff sailDiff = sailDiff;

    public async Task Handle(ExecutionCompletedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.TestCaseExecutionResult.PerformanceTimerResults is null)
        {
            var msg = $"PerformanceTimerResults was null for {notification.TestInstanceContainer.Type.Name}";
            consoleWriter.WriteString(msg, TestMessageLevel.Error);
            throw new SailfishException(msg);
        }

        if (notification.TestInstanceContainer is null)
        {
            var groupRef = notification.TestCaseGroup.FirstOrDefault()?.Cast<TestCase>();

            var msg = $"TestInstanceContainer was null for {groupRef?.Type.Name ?? "UnKnown Type"}";
            consoleWriter.WriteString(msg, TestMessageLevel.Error);
            throw new SailfishException(msg);
        }

        var currentTestCase = notification.TestInstanceContainer.GetTestCaseFromTestCaseGroupMatchingCurrentContainer(notification.TestCaseGroup.Cast<TestCase>());
        if (notification.TestCaseExecutionResult.IsSuccess)
        {
            await HandleSuccessfulTestCase(
                 notification.TestCaseExecutionResult,
                 currentTestCase,
                 new TestClassResultGroup(
                     notification.TestInstanceContainer.Type,
                     [notification.TestCaseExecutionResult]),
                 cancellationToken);
        }
        else
        {
            HandleFailureTestCase(
                notification.TestCaseExecutionResult,
                currentTestCase,
                new TestClassResultGroup(
                    notification.TestInstanceContainer.Type,
                    [notification.TestCaseExecutionResult]),
                cancellationToken);
        }
    }

    private async Task HandleSuccessfulTestCase(
        TestCaseExecutionResult result,
        TestCase currentTestCase,
        TestClassResultGroup classResultGroup,
        CancellationToken cancellationToken)
    {
        var classExecutionSummary = classExecutionSummaryCompiler
            .CompileToSummaries(new List<TestClassResultGroup> { classResultGroup })
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

        testResult.StartTime = result.PerformanceTimerResults?.GetIterationStartTime() ?? new DateTimeOffset();
        testResult.EndTime = result.PerformanceTimerResults?.GetIterationStopTime() ?? new DateTimeOffset();
        testResult.Duration = TimeSpan.FromMilliseconds(double.IsNaN(medianTestRuntime) ? 0 : medianTestRuntime);

        testResult.ErrorMessage = result.Exception?.Message;

        var formattedExecutionSummary = consoleWriter.WriteToConsole(new[] { classExecutionSummary }, []);

        var preloadedLastRunsIfAvailable = await GetLastRun(cancellationToken);
        if (preloadedLastRunsIfAvailable.Count > 0)
        {

            // preloadedLastRun represents an entire tracking file
            foreach (var preloadedLastRun in preloadedLastRunsIfAvailable)
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

        if (result.Exception is not null) testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, result.Exception?.Message));

        consoleWriter.RecordEnd(currentTestCase, testResult.Outcome);
        consoleWriter.RecordResult(testResult);
    }

    private async Task<TrackingFileDataList> GetLastRun(CancellationToken cancellationToken)
    {
        var preloadedLastRunsIfAvailable = new TrackingFileDataList();
        if (!runSettings.DisableAnalysisGlobally && (runSettings.RunScaleFish || runSettings.RunSailDiff))
        {
            try
            {
                var response = await mediator.Send(new GetAllTrackingDataOrderedChronologicallyRequest(), cancellationToken);
                preloadedLastRunsIfAvailable.AddRange(response.TrackingData);
            }
            catch (Exception ex)
            {
                consoleWriter.WriteString(ex.Message);
            }
        }
        return preloadedLastRunsIfAvailable;
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

        testResult.StartTime = result.PerformanceTimerResults?.GetIterationStartTime() ?? new DateTimeOffset();
        testResult.EndTime = result.PerformanceTimerResults?.GetIterationStopTime() ?? new DateTimeOffset();
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
}
