using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using Sailfish.TestAdapter.Execution.Aggregation;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Handlers.TestCaseEvents;

/// <summary>
///     Builds a <see cref="TestCompletionQueueMessage" /> for each finished test case (including any
///     run-over-run SailDiff already applied to its formatted output) and hands it to the
///     <see cref="TestCompletionAggregator" />. The aggregator streams ordinary results immediately and
///     buffers comparison-group members until the group is whole, then runs the cross-method comparison once.
/// </summary>
/// <remarks>
///     This replaces the former intercepting-queue routing (queue vs. direct publishing). There is now a single
///     path: build the message, hand it to the aggregator. The message shape and metadata are unchanged, so the
///     downstream <c>MethodComparisonBatchProcessor</c> and framework publishing behave exactly as before.
/// </remarks>
internal class TestCaseCompletedNotificationHandler : INotificationHandler<TestCaseCompletedNotification>
{
    private readonly TestCompletionAggregator _aggregator;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IRunSettings _runSettings;
    private readonly IAdapterSailDiff _sailDiff;
    private readonly ISailDiffTestOutputWindowMessageFormatter _sailDiffTestOutputWindowMessageFormatter;
    private readonly ISailfishConsoleWindowFormatter _sailfishConsoleWindowFormatter;

    public TestCaseCompletedNotificationHandler(
        ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter,
        ISailDiffTestOutputWindowMessageFormatter sailDiffTestOutputWindowMessageFormatter,
        IRunSettings runSettings,
        IMediator mediator,
        IAdapterSailDiff sailDiff,
        ILogger logger,
        TestCompletionAggregator aggregator)
    {
        _sailfishConsoleWindowFormatter = sailfishConsoleWindowFormatter;
        _sailDiffTestOutputWindowMessageFormatter = sailDiffTestOutputWindowMessageFormatter;
        _runSettings = runSettings;
        _mediator = mediator;
        _sailDiff = sailDiff;
        _logger = logger;
        _aggregator = aggregator;
    }

    public async Task Handle(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        ValidateNotification(notification);
        var message = await CreateCompletionMessage(notification, cancellationToken);
        await _aggregator.ReceiveAsync(message, cancellationToken);
    }

    /// <summary>
    ///     Validates the test case completion notification for required data.
    /// </summary>
    private void ValidateNotification(TestCaseCompletedNotification notification)
    {
        if (notification.TestInstanceContainerExternal is null)
        {
            var groupRef = notification.TestCaseGroup.FirstOrDefault()?.Cast<TestCase>();
            var msg = $"TestInstanceContainer was null for {groupRef?.Type.Name ?? "Unknown Type"}";
            _logger.Log(LogLevel.Error, msg);
            throw new SailfishException(msg);
        }

        if (notification.TestInstanceContainerExternal.PerformanceTimer is null)
        {
            var msg = $"PerformanceTimerResults was null for {notification.TestInstanceContainerExternal.Type.Name}";
            _logger.Log(LogLevel.Error, msg);
            throw new SailfishException(msg);
        }
    }

    /// <summary>
    ///     Builds the test-completion message from the notification: forms the console output, applies run-over-run
    ///     SailDiff when prior runs exist, extracts performance metrics, and packs the metadata the aggregator and
    ///     comparison processor read (TestCase, formatted message, timing, comparison group/role, summaries).
    /// </summary>
    private async Task<TestCompletionQueueMessage> CreateCompletionMessage(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        var classExecutionSummaries = notification.ClassExecutionSummaryTrackingFormat.ToSummaryFormat();
        var testOutputWindowMessage = _sailfishConsoleWindowFormatter.FormConsoleWindowMessageForSailfish([classExecutionSummaries]);

        var testCases = notification
            .TestCaseGroup
            .Select(x => (TestCase)x)
            .ToList();

        var targetDisplayName = notification.TestInstanceContainerExternal!.TestCaseId.DisplayName;

        // Try multiple matching strategies to find the test case
        var currentTestCase = testCases.SingleOrDefault(x => x.FullyQualifiedName.EndsWith(targetDisplayName)) ??
                             testCases.SingleOrDefault(x => x.FullyQualifiedName.EndsWith(targetDisplayName + "()")) ??
                             testCases.SingleOrDefault(x => x.FullyQualifiedName.Contains(targetDisplayName));

        if (currentTestCase == null)
        {
            var availableTestCases = string.Join(", ", testCases.Select(tc => $"'{tc.FullyQualifiedName}'"));
            throw new SailfishException($"Could not find test case matching '{targetDisplayName}'. Available test cases: [{availableTestCases}]");
        }

        var compiledTestCaseResult = classExecutionSummaries.CompiledTestCaseResults.Single();

        // Handle SailDiff analysis if enabled
        var preloadedPreviousRuns = await GetLastRun(cancellationToken);
        if (preloadedPreviousRuns.Count > 0 && !_runSettings.DisableAnalysisGlobally)
        {
            testOutputWindowMessage = RunSailDiff(
                notification.TestInstanceContainerExternal.TestCaseId.DisplayName,
                classExecutionSummaries,
                testOutputWindowMessage,
                preloadedPreviousRuns);
        }

        // Determine test execution status and exception from the (single) compiled result for this case.
        // Using the summary-format result's Exception mirrors the original per-case detection and is reliable
        // after the tracking→summary conversion (GetFailedTestCases on the tracking format is not).
        var exception = compiledTestCaseResult.Exception;
        var statusCode = exception is not null ? StatusCode.Failure : StatusCode.Success;
        var isSuccess = exception is null;

        // Extract performance metrics
        var performanceTimer = notification.TestInstanceContainerExternal.PerformanceTimer!;
        var medianTestRuntime = compiledTestCaseResult.Exception is not null ? 0 :
            (compiledTestCaseResult.PerformanceRunResult?.Median ?? 0);

        var queueMessage = new TestCompletionQueueMessage
        {
            TestCaseId = notification.TestInstanceContainerExternal.TestCaseId.DisplayName,
            CompletedAt = DateTime.UtcNow,
            TestResult = new TestExecutionResult
            {
                IsSuccess = isSuccess,
                ExceptionMessage = exception?.Message,
                ExceptionDetails = exception?.ToString()
            },
            PerformanceMetrics = new PerformanceMetrics
            {
                MedianMs = medianTestRuntime,
                RawExecutionResults = compiledTestCaseResult.PerformanceRunResult?.RawExecutionResults?.ToArray() ?? Array.Empty<double>(),
                DataWithOutliersRemoved = compiledTestCaseResult.PerformanceRunResult?.DataWithOutliersRemoved?.ToArray() ?? Array.Empty<double>(),
                LowerOutliers = compiledTestCaseResult.PerformanceRunResult?.LowerOutliers?.ToArray() ?? Array.Empty<double>(),
                UpperOutliers = compiledTestCaseResult.PerformanceRunResult?.UpperOutliers?.ToArray() ?? Array.Empty<double>()
            },
            Metadata = new Dictionary<string, object>
            {
                ["TestCase"] = currentTestCase,
                ["FormattedMessage"] = testOutputWindowMessage,
                ["StartTime"] = performanceTimer.GetIterationStartTime(),
                ["EndTime"] = performanceTimer.GetIterationStopTime(),
                ["MedianRuntime"] = medianTestRuntime,
                ["StatusCode"] = statusCode,
                ["Exception"] = exception as object ?? DBNull.Value,
                ["ClassExecutionSummaries"] = classExecutionSummaries,
                ["CompiledTestCaseResult"] = compiledTestCaseResult,
                ["TestCaseGroup"] = notification.TestCaseGroup,
                ["RunSettings"] = _runSettings,
                ["ComparisonGroup"] = ExtractComparisonGroup(currentTestCase) ?? (object)DBNull.Value,
                ["ComparisonRole"] = ExtractComparisonRole(currentTestCase) ?? (object)DBNull.Value
            }
        };

        return queueMessage;
    }

    private string RunSailDiff(
        string testCaseDisplayName,
        IClassExecutionSummary classExecutionSummary,
        string testOutputWindowMessage,
        TrackingFileDataList preloadedLastRunsIfAvailable)
    {
        var preloadedRun = preloadedLastRunsIfAvailable.FindFirstMatchingTestCaseId(new TestCaseId(testCaseDisplayName));
        if (preloadedRun is null) return testOutputWindowMessage;

        var testCaseResults = _sailDiff.ComputeTestCaseDiff(
            [testCaseDisplayName],
            [testCaseDisplayName],
            testCaseDisplayName,
            classExecutionSummary,
            preloadedRun.PerformanceRunResult!);

        testOutputWindowMessage = AttachSailDiffResultMessage(testOutputWindowMessage, testCaseResults);
        return testOutputWindowMessage;
    }

    private string AttachSailDiffResultMessage(string testOutputWindowMessage, TestCaseSailDiffResult testCaseResults)
    {
        if (testCaseResults.SailDiffResults.Count > 0)
        {
            var sailDiffTestOutputString = _sailDiffTestOutputWindowMessageFormatter
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
        if (_runSettings.DisableAnalysisGlobally || _runSettings is { RunScaleFish: false, RunSailDiff: false }) return preloadedLastRunsIfAvailable;

        try
        {
            var response = await _mediator.Send(
                new GetAllTrackingDataOrderedChronologicallyRequest(),
                cancellationToken);
            preloadedLastRunsIfAvailable.AddRange(response.TrackingData.Skip(1)); // the most recent is the current run
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex.Message);
        }

        return preloadedLastRunsIfAvailable;
    }

    private string? ExtractComparisonGroup(TestCase testCase)
    {
        try
        {
            return testCase.GetPropertyValue<string>(SailfishManagedProperty.SailfishComparisonGroupProperty, null);
        }
        catch
        {
            return null;
        }
    }

    private string? ExtractComparisonRole(TestCase testCase)
    {
        try
        {
            return testCase.GetPropertyValue<string>(SailfishManagedProperty.SailfishComparisonRoleProperty, null);
        }
        catch
        {
            return null;
        }
    }
}
