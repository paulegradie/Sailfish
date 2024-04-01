using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Requests;
using Sailfish.Exceptions;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Handlers.TestCaseEvents;

internal class TestCaseCompletedNotificationHandler : INotificationHandler<TestCaseCompletedNotification>
{
    private readonly ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter;
    private readonly ISailDiffTestOutputWindowMessageFormatter sailDiffTestOutputWindowMessageFormatter;
    private readonly IRunSettings runSettings;
    private readonly IMediator mediator;
    private readonly IAdapterSailDiff sailDiff;
    private readonly ILogger logger;

    public TestCaseCompletedNotificationHandler(
        ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter,
        ISailDiffTestOutputWindowMessageFormatter sailDiffTestOutputWindowMessageFormatter,
        IRunSettings runSettings,
        IMediator mediator,
        IAdapterSailDiff sailDiff,
        ILogger logger)
    {
        this.sailfishConsoleWindowFormatter = sailfishConsoleWindowFormatter;
        this.sailDiffTestOutputWindowMessageFormatter = sailDiffTestOutputWindowMessageFormatter;
        this.runSettings = runSettings;
        this.mediator = mediator;
        this.sailDiff = sailDiff;
        this.logger = logger;
    }

    public async Task Handle(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.TestInstanceContainerExternal.PerformanceTimer is null)
        {
            var msg = $"PerformanceTimerResults was null for {notification.TestInstanceContainerExternal.Type.Name}";
            logger.Log(LogLevel.Error, msg);
            throw new SailfishException(msg);
        }

        if (notification.TestInstanceContainerExternal is null)
        {
            var groupRef = notification.TestCaseGroup.FirstOrDefault()?.Cast<TestCase>();

            var msg = $"TestInstanceContainer was null for {groupRef?.Type.Name ?? "UnKnown Type"}";
            logger.Log(LogLevel.Error, msg);
            throw new SailfishException(msg);
        }

        var classExecutionSummaries = notification.ClassExecutionSummaryTrackingFormat.ToSummaryFormat();
        var testOutputWindowMessage = sailfishConsoleWindowFormatter.FormConsoleWindowMessageForSailfish([classExecutionSummaries]);

        var currentTestCase = notification
            .TestCaseGroup
            .Select(x => (TestCase)x)
            .Single(x => x.FullyQualifiedName.EndsWith(notification.TestInstanceContainerExternal.TestCaseId.DisplayName));


        var compiledTestCaseResult = classExecutionSummaries.CompiledTestCaseResults.Single();
        if (compiledTestCaseResult.Exception is not null)
        {
            await mediator.Publish(new FrameworkTestCaseEndNotification(
                testOutputWindowMessage,
                notification.TestInstanceContainerExternal.PerformanceTimer.GetIterationStartTime(),
                notification.TestInstanceContainerExternal.PerformanceTimer.GetIterationStopTime(),
                0,
                currentTestCase,
                StatusCode.Failure,
                compiledTestCaseResult.Exception
            ), cancellationToken);
            return;
        }

        var medianTestRuntime = compiledTestCaseResult.PerformanceRunResult?.Median ?? throw new SailfishException("Error computing compiled results");

        var preloadedPreviousRuns = await GetLastRun(cancellationToken);
        if (preloadedPreviousRuns.Count > 0 && !runSettings.DisableAnalysisGlobally)
        {
            testOutputWindowMessage = RunSailDiff(
                notification.TestInstanceContainerExternal.TestCaseId.DisplayName,
                classExecutionSummaries,
                testOutputWindowMessage,
                preloadedPreviousRuns);
        }

        var exception = notification.ClassExecutionSummaryTrackingFormat.GetFailedTestCases().Any()
            ? notification.ClassExecutionSummaryTrackingFormat.GetFailedTestCases().Single().Exception
            : null;

        var statusCode = notification.ClassExecutionSummaryTrackingFormat.GetFailedTestCases().Any() ? StatusCode.Failure : StatusCode.Success;

        await mediator.Publish(new FrameworkTestCaseEndNotification(
            testOutputWindowMessage,
            notification.TestInstanceContainerExternal.PerformanceTimer.GetIterationStartTime(),
            notification.TestInstanceContainerExternal.PerformanceTimer.GetIterationStopTime(),
            medianTestRuntime,
            currentTestCase,
            statusCode,
            exception
        ), cancellationToken);
    }

    private string RunSailDiff(
        string testCaseDisplayName,
        IClassExecutionSummary classExecutionSummary,
        string testOutputWindowMessage,
        TrackingFileDataList preloadedLastRunsIfAvailable)
    {
        // preloadedLastRun represents an entire tracking file
        var preloadedRuns = preloadedLastRunsIfAvailable
            .Select(preloadedLastRun =>
                preloadedLastRun
                    .SelectMany(x => x.CompiledTestCaseResults)
                    .SingleOrDefault(x => x.TestCaseId?.DisplayName == testCaseDisplayName));

        foreach (var preloadedSummaryMatchingCurrentSummary in preloadedRuns)
        {
            if (preloadedSummaryMatchingCurrentSummary?.PerformanceRunResult is null) continue;

            // if we eventually find a previous run (we don't discriminate by age of run -- perhaps we should
            var testCaseResults = sailDiff.ComputeTestCaseDiff(
                [testCaseDisplayName],
                [testCaseDisplayName],
                testCaseDisplayName,
                classExecutionSummary,
                preloadedSummaryMatchingCurrentSummary.PerformanceRunResult);

            testOutputWindowMessage = AttachSailDiffResultMessage(testOutputWindowMessage, testCaseResults);

            break;
        }

        return testOutputWindowMessage;
    }

    private string AttachSailDiffResultMessage(string testOutputWindowMessage, TestCaseSailDiffResult testCaseResults)
    {
        if (testCaseResults.SailDiffResults.Count > 0)
        {
            var sailDiffTestOutputString = sailDiffTestOutputWindowMessageFormatter
                .FormTestOutputWindowMessageForSailDiff(
                    testCaseResults.SailDiffResults.Single(),
                    testCaseResults.TestIds,
                    testCaseResults.TestSettings);
            testOutputWindowMessage += "\n" + sailDiffTestOutputString;
        }
        else
        {
            testOutputWindowMessage += "\n" + "Current or previous runs not suitable for statistical testing";
        }

        return testOutputWindowMessage;
    }

    private async Task<TrackingFileDataList> GetLastRun(CancellationToken cancellationToken)
    {
        var preloadedLastRunsIfAvailable = new TrackingFileDataList();
        if (runSettings.DisableAnalysisGlobally || runSettings is { RunScaleFish: false, RunSailDiff: false })
        {
            return preloadedLastRunsIfAvailable;
        }

        try
        {
            var response = await mediator.Send(
                new GetAllTrackingDataOrderedChronologicallyRequest(),
                cancellationToken);
            preloadedLastRunsIfAvailable.AddRange(response.TrackingData.Skip(1)); // the most recent is the current run
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Warning, ex.Message);
        }

        return preloadedLastRunsIfAvailable;
    }
}