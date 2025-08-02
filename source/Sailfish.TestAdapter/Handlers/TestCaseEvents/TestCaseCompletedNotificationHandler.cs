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
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Configuration;
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Handlers.TestCaseEvents;

/// <summary>
/// Handles test case completion notifications in the intercepting queue architecture.
/// This handler is responsible for either publishing test completion messages to the queue
/// for asynchronous processing or falling back to direct framework publishing when the
/// queue system is disabled or fails.
/// </summary>
/// <remarks>
/// This handler implements the intercepting queue architecture where test completion
/// notifications are processed through a queue system that enables batch processing,
/// cross-test-case analysis, and enhanced result generation before results reach the
/// VS Test Platform.
///
/// Key architectural responsibilities:
/// - Route test completion notifications to queue or direct publishing based on configuration
/// - Create TestCompletionQueueMessage objects with all relevant test execution data
/// - Add test cases to appropriate batches for cross-test-case analysis
/// - Provide robust fallback mechanism to ensure test results are never lost
/// - Maintain backward compatibility with existing direct publishing behavior
///
/// The handler supports two execution modes:
/// 1. Queue Mode (when QueueConfiguration.IsEnabled = true):
///    - Creates TestCompletionQueueMessage with test execution data
///    - Adds test case to appropriate batch via ITestCaseBatchingService
///    - Publishes message to queue via ITestCompletionQueuePublisher
///    - Falls back to direct publishing if queue operations fail
///
/// 2. Direct Mode (when QueueConfiguration.IsEnabled = false):
///    - Uses original direct framework publishing behavior
///    - Maintains full backward compatibility
///    - No queue overhead for existing workflows
///
/// Thread Safety:
/// This handler is designed to be thread-safe and handle concurrent test execution
/// scenarios where multiple test cases complete simultaneously.
/// </remarks>
internal class TestCaseCompletedNotificationHandler : INotificationHandler<TestCaseCompletedNotification>
{
    #region Private Fields

    private readonly ILogger logger;
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly IAdapterSailDiff sailDiff;
    private readonly ISailDiffTestOutputWindowMessageFormatter sailDiffTestOutputWindowMessageFormatter;
    private readonly ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter;
    private readonly QueueConfiguration queueConfiguration;
    private readonly ITestCompletionQueuePublisher? queuePublisher;
    private readonly ITestCaseBatchingService? batchingService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the TestCaseCompletedNotificationHandler class.
    /// </summary>
    /// <param name="sailfishConsoleWindowFormatter">Service for formatting console window messages.</param>
    /// <param name="sailDiffTestOutputWindowMessageFormatter">Service for formatting SailDiff test output messages.</param>
    /// <param name="runSettings">Current run settings configuration.</param>
    /// <param name="mediator">MediatR mediator for publishing notifications.</param>
    /// <param name="sailDiff">Service for computing test case differences.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="queueConfiguration">Configuration settings for the queue system.</param>
    /// <param name="queuePublisher">Optional queue publisher service (null when queue is disabled).</param>
    /// <param name="batchingService">Optional batching service (null when queue is disabled).</param>
    /// <remarks>
    /// The queue-related dependencies (queuePublisher and batchingService) are optional
    /// and will be null when the queue system is disabled. This allows the handler to
    /// function normally in both queue and direct publishing modes.
    /// </remarks>
    public TestCaseCompletedNotificationHandler(
        ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter,
        ISailDiffTestOutputWindowMessageFormatter sailDiffTestOutputWindowMessageFormatter,
        IRunSettings runSettings,
        IMediator mediator,
        IAdapterSailDiff sailDiff,
        ILogger logger,
        QueueConfiguration queueConfiguration,
        ITestCompletionQueuePublisher? queuePublisher = null,
        ITestCaseBatchingService? batchingService = null)
    {
        this.sailfishConsoleWindowFormatter = sailfishConsoleWindowFormatter;
        this.sailDiffTestOutputWindowMessageFormatter = sailDiffTestOutputWindowMessageFormatter;
        this.runSettings = runSettings;
        this.mediator = mediator;
        this.sailDiff = sailDiff;
        this.logger = logger;
        this.queueConfiguration = queueConfiguration;
        this.queuePublisher = queuePublisher;
        this.batchingService = batchingService;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Handles test case completion notifications using the intercepting queue architecture.
    /// Routes notifications to either queue processing or direct framework publishing based
    /// on configuration settings.
    /// </summary>
    /// <param name="notification">The test case completion notification containing execution results.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="SailfishException">Thrown when required notification data is missing or invalid.</exception>
    /// <remarks>
    /// This method implements the core routing logic for the intercepting queue architecture:
    ///
    /// 1. Validates notification data (TestInstanceContainer and PerformanceTimer)
    /// 2. Routes to queue processing if queue is enabled and available
    /// 3. Falls back to direct framework publishing if queue is disabled or fails
    ///
    /// The method ensures that test results are never lost by providing robust fallback
    /// mechanisms and comprehensive error handling.
    /// </remarks>
    public async Task Handle(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        // Validate required notification data
        ValidateNotification(notification);

        // Route to appropriate processing method based on queue configuration
        if (queueConfiguration.IsEnabled && queuePublisher != null && batchingService != null)
        {
            logger.Log(LogLevel.Debug,
                "Queue system is enabled. Processing test case '{0}' through queue.",
                notification.TestInstanceContainerExternal!.TestCaseId.DisplayName);

            await HandleWithQueue(notification, cancellationToken);
        }
        else
        {
            logger.Log(LogLevel.Debug,
                "Queue system is disabled. Using direct framework publishing for test case '{0}'.",
                notification.TestInstanceContainerExternal!.TestCaseId.DisplayName);

            await HandleDirectPublishing(notification, cancellationToken);
        }
    }

    #endregion

    #region Private Methods - Queue Processing

    /// <summary>
    /// Handles test case completion through the queue system with fallback to direct publishing.
    /// </summary>
    /// <param name="notification">The test case completion notification.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method implements the queue processing path of the intercepting architecture:
    /// 1. Creates TestCompletionQueueMessage from notification data
    /// 2. Adds test case to appropriate batch for cross-test-case analysis
    /// 3. Publishes message to queue for asynchronous processing
    /// 4. Falls back to direct publishing if queue operations fail and fallback is enabled
    /// </remarks>
    private async Task HandleWithQueue(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            // Create queue message with all test execution data
            var queueMessage = await CreateQueueMessage(notification, cancellationToken);

            logger.Log(LogLevel.Debug,
                "Created queue message for test case '{0}' with {1} metadata entries.",
                queueMessage.TestCaseId, queueMessage.Metadata.Count);

            // Add test case to appropriate batch for cross-test-case analysis
            var batchId = await batchingService!.AddTestCaseToBatchAsync(queueMessage, cancellationToken);

            logger.Log(LogLevel.Debug,
                "Added test case '{0}' to batch '{1}' for cross-test-case analysis.",
                queueMessage.TestCaseId, batchId);

            // Publish message to queue for asynchronous processing
            await queuePublisher!.PublishTestCompletion(queueMessage, cancellationToken);

            logger.Log(LogLevel.Information,
                "Successfully published test case '{0}' to queue for processing.",
                queueMessage.TestCaseId);
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Error, ex,
                "Failed to process test case '{0}' through queue: {1}",
                notification.TestInstanceContainerExternal!.TestCaseId.DisplayName, ex.Message);

            // Fall back to direct publishing if enabled
            if (queueConfiguration.EnableFallbackPublishing)
            {
                logger.Log(LogLevel.Warning,
                    "Falling back to direct framework publishing for test case '{0}' due to queue failure.",
                    notification.TestInstanceContainerExternal.TestCaseId.DisplayName);

                await HandleDirectPublishing(notification, cancellationToken);
            }
            else
            {
                logger.Log(LogLevel.Error,
                    "Queue processing failed and fallback is disabled. Test case '{0}' results may be lost.",
                    notification.TestInstanceContainerExternal.TestCaseId.DisplayName);

                // Re-throw to maintain error contract when fallback is disabled
                throw;
            }
        }
    }

    #endregion

    #region Private Methods - Direct Publishing

    /// <summary>
    /// Handles test case completion using direct framework publishing (original behavior).
    /// This method maintains the original TestCaseCompletedNotificationHandler logic for
    /// backward compatibility and fallback scenarios.
    /// </summary>
    /// <param name="notification">The test case completion notification.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method preserves the exact original behavior of the TestCaseCompletedNotificationHandler
    /// to ensure backward compatibility when the queue system is disabled or when fallback
    /// is triggered due to queue failures.
    /// </remarks>
    private async Task HandleDirectPublishing(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        var classExecutionSummaries = notification.ClassExecutionSummaryTrackingFormat.ToSummaryFormat();
        var testOutputWindowMessage = sailfishConsoleWindowFormatter.FormConsoleWindowMessageForSailfish([classExecutionSummaries]);

        var currentTestCase = notification
            .TestCaseGroup
            .Select(x => (TestCase)x)
            .Single(x => x.FullyQualifiedName.EndsWith(notification.TestInstanceContainerExternal!.TestCaseId.DisplayName));

        var compiledTestCaseResult = classExecutionSummaries.CompiledTestCaseResults.Single();
        if (compiledTestCaseResult.Exception is not null)
        {
            await mediator.Publish(new FrameworkTestCaseEndNotification(
                testOutputWindowMessage,
                notification.TestInstanceContainerExternal.PerformanceTimer!.GetIterationStartTime(),
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
            testOutputWindowMessage = RunSailDiff(
                notification.TestInstanceContainerExternal.TestCaseId.DisplayName,
                classExecutionSummaries,
                testOutputWindowMessage,
                preloadedPreviousRuns);

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

    #endregion

    #region Private Methods - Helper Methods

    /// <summary>
    /// Validates the test case completion notification for required data.
    /// </summary>
    /// <param name="notification">The notification to validate.</param>
    /// <exception cref="SailfishException">Thrown when required data is missing.</exception>
    private void ValidateNotification(TestCaseCompletedNotification notification)
    {
        if (notification.TestInstanceContainerExternal is null)
        {
            var groupRef = notification.TestCaseGroup.FirstOrDefault()?.Cast<TestCase>();
            var msg = $"TestInstanceContainer was null for {groupRef?.Type.Name ?? "Unknown Type"}";
            logger.Log(LogLevel.Error, msg);
            throw new SailfishException(msg);
        }

        if (notification.TestInstanceContainerExternal.PerformanceTimer is null)
        {
            var msg = $"PerformanceTimerResults was null for {notification.TestInstanceContainerExternal.Type.Name}";
            logger.Log(LogLevel.Error, msg);
            throw new SailfishException(msg);
        }
    }

    /// <summary>
    /// Creates a TestCompletionQueueMessage from the test case completion notification.
    /// This method extracts all relevant test execution data and packages it for queue processing.
    /// </summary>
    /// <param name="notification">The test case completion notification.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A TestCompletionQueueMessage containing all test execution data.</returns>
    /// <remarks>
    /// This method performs the critical data mapping between the notification system and
    /// the queue system. It extracts:
    /// - Test case identification and metadata
    /// - Performance metrics and timing information
    /// - Test execution results and status
    /// - Formatted output messages for display
    /// - Exception details for failed tests
    /// - All metadata required for framework publishing processor
    ///
    /// The created message contains all data necessary for queue processors to reconstruct
    /// the original FrameworkTestCaseEndNotification for VS Test Platform reporting.
    /// </remarks>
    private async Task<TestCompletionQueueMessage> CreateQueueMessage(TestCaseCompletedNotification notification, CancellationToken cancellationToken)
    {
        // Process notification data (same as direct publishing logic)
        var classExecutionSummaries = notification.ClassExecutionSummaryTrackingFormat.ToSummaryFormat();
        var testOutputWindowMessage = sailfishConsoleWindowFormatter.FormConsoleWindowMessageForSailfish([classExecutionSummaries]);

        var currentTestCase = notification
            .TestCaseGroup
            .Select(x => (TestCase)x)
            .Single(x => x.FullyQualifiedName.EndsWith(notification.TestInstanceContainerExternal!.TestCaseId.DisplayName));

        var compiledTestCaseResult = classExecutionSummaries.CompiledTestCaseResults.Single();

        // Handle SailDiff analysis if enabled
        var preloadedPreviousRuns = await GetLastRun(cancellationToken);
        if (preloadedPreviousRuns.Count > 0 && !runSettings.DisableAnalysisGlobally)
        {
            testOutputWindowMessage = RunSailDiff(
                notification.TestInstanceContainerExternal.TestCaseId.DisplayName,
                classExecutionSummaries,
                testOutputWindowMessage,
                preloadedPreviousRuns);
        }

        // Determine test execution status and exception
        var hasFailures = notification.ClassExecutionSummaryTrackingFormat.GetFailedTestCases().Any();
        var exception = hasFailures ? notification.ClassExecutionSummaryTrackingFormat.GetFailedTestCases().Single().Exception : null;
        var statusCode = hasFailures ? StatusCode.Failure : StatusCode.Success;
        var isSuccess = statusCode == StatusCode.Success;

        // Extract performance metrics
        var performanceTimer = notification.TestInstanceContainerExternal.PerformanceTimer!;
        var medianTestRuntime = compiledTestCaseResult.Exception is not null ? 0 :
            (compiledTestCaseResult.PerformanceRunResult?.Median ?? 0);

        // Create the queue message
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
                // Extract raw execution results if available
                RawExecutionResults = compiledTestCaseResult.PerformanceRunResult?.RawExecutionResults?.ToArray() ?? Array.Empty<double>(),
                DataWithOutliersRemoved = compiledTestCaseResult.PerformanceRunResult?.DataWithOutliersRemoved?.ToArray() ?? Array.Empty<double>(),
                LowerOutliers = compiledTestCaseResult.PerformanceRunResult?.LowerOutliers?.ToArray() ?? Array.Empty<double>(),
                UpperOutliers = compiledTestCaseResult.PerformanceRunResult?.UpperOutliers?.ToArray() ?? Array.Empty<double>()
            },
            Metadata = new Dictionary<string, object>
            {
                // Core data required for framework publishing
                ["TestCase"] = currentTestCase,
                ["FormattedMessage"] = testOutputWindowMessage,
                ["StartTime"] = performanceTimer.GetIterationStartTime(),
                ["EndTime"] = performanceTimer.GetIterationStopTime(),
                ["MedianRuntime"] = medianTestRuntime,
                ["StatusCode"] = statusCode,

                // Exception data (if any)
                ["Exception"] = exception as object ?? DBNull.Value,

                // Additional context for processors
                ["ClassExecutionSummaries"] = classExecutionSummaries,
                ["CompiledTestCaseResult"] = compiledTestCaseResult,
                ["TestCaseGroup"] = notification.TestCaseGroup,
                ["RunSettings"] = runSettings,

                // Comparison metadata (if present)
                ["ComparisonGroup"] = ExtractComparisonGroup(currentTestCase) ?? (object)DBNull.Value,
                ["ComparisonRole"] = ExtractComparisonRole(currentTestCase) ?? (object)DBNull.Value
            }
        };

        logger.Log(LogLevel.Debug,
            "Created queue message for test case '{0}': Success={1}, MedianMs={2}, MetadataCount={3}",
            queueMessage.TestCaseId, queueMessage.TestResult.IsSuccess, queueMessage.PerformanceMetrics.MedianMs, queueMessage.Metadata.Count);

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

        var testCaseResults = sailDiff.ComputeTestCaseDiff(
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
        if (runSettings.DisableAnalysisGlobally || runSettings is { RunScaleFish: false, RunSailDiff: false }) return preloadedLastRunsIfAvailable;

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

    /// <summary>
    /// Extracts the comparison group from a test case's properties.
    /// </summary>
    /// <param name="testCase">The test case to extract from.</param>
    /// <returns>The comparison group name, or null if not found.</returns>
    private string? ExtractComparisonGroup(TestCase testCase)
    {
        try
        {
            var value = testCase.GetPropertyValue<string>(SailfishManagedProperty.SailfishComparisonGroupProperty, null);
            return value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the comparison role from a test case's properties.
    /// </summary>
    /// <param name="testCase">The test case to extract from.</param>
    /// <returns>The comparison role (Before/After), or null if not found.</returns>
    private string? ExtractComparisonRole(TestCase testCase)
    {
        try
        {
            var value = testCase.GetPropertyValue<string>(SailfishManagedProperty.SailfishComparisonRoleProperty, null);
            return value;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}