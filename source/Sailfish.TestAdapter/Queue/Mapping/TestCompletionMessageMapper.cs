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
using Sailfish.TestAdapter.Queue.Contracts;
using Sailfish.TestAdapter.TestProperties;

namespace Sailfish.TestAdapter.Queue.Mapping;

/// <summary>
/// Service responsible for mapping test case completion notifications to queue messages
/// with comprehensive metadata for batch processing and cross-test-case analysis.
/// </summary>
/// <remarks>
/// The TestCompletionMessageMapper extracts all relevant test execution data from
/// TestCaseCompletedNotification objects and transforms them into TestCompletionQueueMessage
/// objects suitable for queue processing. This includes performance metrics, test results,
/// formatted output messages, and metadata required for batching and framework publishing.
/// 
/// Key Features:
/// - Comprehensive data extraction from test completion notifications
/// - Integration with SailDiff analysis for performance comparison
/// - Batching metadata generation for cross-test-case analysis
/// - Thread-safe operations for concurrent test execution
/// - Robust error handling and validation
/// - Support for all existing Sailfish test execution features
/// 
/// The mapper generates metadata for various batching strategies:
/// - Test class and assembly information for class-based batching
/// - Comparison group identifiers for attribute-based batching
/// - Custom criteria extraction for flexible batching strategies
/// - Execution context data for environment-based grouping
/// - Performance profile information for performance-based batching
/// 
/// Thread Safety:
/// This service is designed to be thread-safe and stateless, allowing concurrent
/// mapping operations during test execution when multiple test cases complete
/// simultaneously.
/// </remarks>
internal class TestCompletionMessageMapper : ITestCompletionMessageMapper
{
    #region Private Fields

    private readonly ILogger logger;
    private readonly ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter;
    private readonly ISailDiffTestOutputWindowMessageFormatter sailDiffTestOutputWindowMessageFormatter;
    private readonly IAdapterSailDiff sailDiff;
    private readonly IRunSettings runSettings;
    private readonly IMediator mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the TestCompletionMessageMapper class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output and error reporting.</param>
    /// <param name="sailfishConsoleWindowFormatter">Service for formatting console window messages.</param>
    /// <param name="sailDiffTestOutputWindowMessageFormatter">Service for formatting SailDiff test output messages.</param>
    /// <param name="sailDiff">Service for computing test case differences and performance analysis.</param>
    /// <param name="runSettings">Current run settings configuration.</param>
    /// <param name="mediator">MediatR mediator for accessing historical test data.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the required dependencies is null.
    /// </exception>
    public TestCompletionMessageMapper(
        ILogger logger,
        ISailfishConsoleWindowFormatter sailfishConsoleWindowFormatter,
        ISailDiffTestOutputWindowMessageFormatter sailDiffTestOutputWindowMessageFormatter,
        IAdapterSailDiff sailDiff,
        IRunSettings runSettings,
        IMediator mediator)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.sailfishConsoleWindowFormatter = sailfishConsoleWindowFormatter ?? throw new ArgumentNullException(nameof(sailfishConsoleWindowFormatter));
        this.sailDiffTestOutputWindowMessageFormatter = sailDiffTestOutputWindowMessageFormatter ?? throw new ArgumentNullException(nameof(sailDiffTestOutputWindowMessageFormatter));
        this.sailDiff = sailDiff ?? throw new ArgumentNullException(nameof(sailDiff));
        this.runSettings = runSettings ?? throw new ArgumentNullException(nameof(runSettings));
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Maps a test case completion notification to a queue message with comprehensive
    /// metadata for batch processing and cross-test-case analysis.
    /// </summary>
    /// <param name="notification">The test case completion notification containing execution results.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A TestCompletionQueueMessage containing all test execution data and metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when notification is null.</exception>
    /// <exception cref="SailfishException">Thrown when required notification data is missing.</exception>
    public async Task<TestCompletionQueueMessage> MapToQueueMessageAsync(
        TestCaseCompletedNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (notification == null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        logger.Log(LogLevel.Debug,
            "Starting mapping of test case completion notification to queue message for test: {0}",
            notification.TestInstanceContainerExternal?.TestCaseId?.DisplayName ?? "Unknown");

        try
        {
            // Validate required notification data
            ValidateNotification(notification);

            // Extract core test execution data
            var coreData = ExtractCoreTestData(notification);

            // Process SailDiff analysis if enabled
            var formattedMessage = await ProcessSailDiffAnalysis(
                coreData.TestOutputWindowMessage,
                coreData.ClassExecutionSummaries,
                notification.TestInstanceContainerExternal!.TestCaseId.DisplayName,
                cancellationToken);

            // Determine test execution status and results
            var executionResult = DetermineExecutionResult(notification);

            // Extract performance metrics
            var performanceMetrics = ExtractPerformanceMetrics(coreData.CompiledTestCaseResult, notification);

            // Generate batching metadata for cross-test-case analysis
            var batchingMetadata = ExtractBatchingMetadata(notification, coreData.CurrentTestCase);

            // Create the comprehensive queue message
            var queueMessage = CreateQueueMessage(
                notification,
                coreData,
                formattedMessage,
                executionResult,
                performanceMetrics,
                batchingMetadata);

            logger.Log(LogLevel.Information,
                "Successfully mapped test case '{0}' to queue message with {1} metadata entries",
                queueMessage.TestCaseId, queueMessage.Metadata.Count);

            return queueMessage;
        }
        catch (Exception ex) when (!(ex is ArgumentNullException || ex is SailfishException))
        {
            logger.Log(LogLevel.Error, ex,
                "Failed to map test case completion notification to queue message for test '{0}': {1}",
                notification.TestInstanceContainerExternal?.TestCaseId?.DisplayName ?? "Unknown", ex.Message);

            throw new SailfishException(
                $"Failed to map test case completion notification to queue message: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods - Core Data Extraction

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
    /// Extracts core test execution data from the notification.
    /// </summary>
    /// <param name="notification">The test case completion notification.</param>
    /// <returns>A data structure containing core test execution information.</returns>
    private CoreTestData ExtractCoreTestData(TestCaseCompletedNotification notification)
    {
        var classExecutionSummaries = notification.ClassExecutionSummaryTrackingFormat.ToSummaryFormat();
        var testOutputWindowMessage = sailfishConsoleWindowFormatter.FormConsoleWindowMessageForSailfish([classExecutionSummaries]);

        var currentTestCase = notification
            .TestCaseGroup
            .Select(x => (TestCase)x)
            .Single(x => x.FullyQualifiedName.EndsWith(notification.TestInstanceContainerExternal!.TestCaseId.DisplayName));

        var compiledTestCaseResult = classExecutionSummaries.CompiledTestCaseResults.Single();

        return new CoreTestData
        {
            ClassExecutionSummaries = classExecutionSummaries,
            TestOutputWindowMessage = testOutputWindowMessage,
            CurrentTestCase = currentTestCase,
            CompiledTestCaseResult = compiledTestCaseResult
        };
    }

    #endregion

    #region Private Methods - SailDiff Analysis

    /// <summary>
    /// Processes SailDiff analysis if enabled and integrates results into the formatted message.
    /// </summary>
    /// <param name="testOutputWindowMessage">The base test output message.</param>
    /// <param name="classExecutionSummaries">The class execution summaries.</param>
    /// <param name="testCaseDisplayName">The test case display name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The formatted message with SailDiff analysis if applicable.</returns>
    private async Task<string> ProcessSailDiffAnalysis(
        string testOutputWindowMessage,
        IClassExecutionSummary classExecutionSummaries,
        string testCaseDisplayName,
        CancellationToken cancellationToken)
    {
        try
        {
            var preloadedPreviousRuns = await GetLastRunAsync(cancellationToken);
            if (preloadedPreviousRuns.Count > 0 && !runSettings.DisableAnalysisGlobally)
            {
                return RunSailDiff(
                    testCaseDisplayName,
                    classExecutionSummaries,
                    testOutputWindowMessage,
                    preloadedPreviousRuns);
            }

            return testOutputWindowMessage;
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Warning, ex,
                "Failed to process SailDiff analysis for test case '{0}': {1}",
                testCaseDisplayName, ex.Message);

            // Return original message if SailDiff analysis fails
            return testOutputWindowMessage;
        }
    }

    /// <summary>
    /// Runs SailDiff analysis and integrates results into the test output message.
    /// </summary>
    /// <param name="testCaseDisplayName">The test case display name.</param>
    /// <param name="classExecutionSummary">The class execution summary.</param>
    /// <param name="testOutputWindowMessage">The base test output message.</param>
    /// <param name="preloadedLastRunsIfAvailable">Previous run data for comparison.</param>
    /// <returns>The test output message with SailDiff results integrated.</returns>
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

        return AttachSailDiffResultMessage(testOutputWindowMessage, testCaseResults);
    }

    /// <summary>
    /// Attaches SailDiff result message to the test output.
    /// </summary>
    /// <param name="testOutputWindowMessage">The base test output message.</param>
    /// <param name="testCaseResults">The SailDiff test case results.</param>
    /// <returns>The combined test output message with SailDiff results.</returns>
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

    /// <summary>
    /// Retrieves the last run data for SailDiff analysis.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of previous run tracking data.</returns>
    private async Task<TrackingFileDataList> GetLastRunAsync(CancellationToken cancellationToken)
    {
        var preloadedLastRunsIfAvailable = new TrackingFileDataList();
        if (runSettings.DisableAnalysisGlobally || runSettings is { RunScaleFish: false, RunSailDiff: false })
            return preloadedLastRunsIfAvailable;

        try
        {
            var response = await mediator.Send(
                new GetAllTrackingDataOrderedChronologicallyRequest(),
                cancellationToken);
            preloadedLastRunsIfAvailable.AddRange(response.TrackingData.Skip(1)); // the most recent is the current run
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Warning, ex,
                "Failed to retrieve previous run data for SailDiff analysis: {0}", ex.Message);
        }

        return preloadedLastRunsIfAvailable;
    }

    #endregion

    #region Private Methods - Result Processing

    /// <summary>
    /// Determines the test execution result and status information.
    /// </summary>
    /// <param name="notification">The test case completion notification.</param>
    /// <returns>Execution result data including success status and exception details.</returns>
    private ExecutionResultData DetermineExecutionResult(TestCaseCompletedNotification notification)
    {
        var hasFailures = notification.ClassExecutionSummaryTrackingFormat.GetFailedTestCases().Any();
        var exception = hasFailures ? notification.ClassExecutionSummaryTrackingFormat.GetFailedTestCases().Single().Exception : null;
        var statusCode = hasFailures ? StatusCode.Failure : StatusCode.Success;
        var isSuccess = statusCode == StatusCode.Success;

        // Calculate median runtime, handling exception cases
        var classExecutionSummaries = notification.ClassExecutionSummaryTrackingFormat.ToSummaryFormat();
        var compiledTestCaseResult = classExecutionSummaries.CompiledTestCaseResults.Single();
        var medianTestRuntime = compiledTestCaseResult.Exception is not null ? 0 :
            (compiledTestCaseResult.PerformanceRunResult?.Median ?? 0);

        return new ExecutionResultData
        {
            IsSuccess = isSuccess,
            Exception = exception,
            StatusCode = statusCode,
            MedianRuntime = medianTestRuntime
        };
    }

    #endregion

    #region Private Methods - Performance Metrics

    /// <summary>
    /// Extracts comprehensive performance metrics from the test execution results.
    /// </summary>
    /// <param name="compiledTestCaseResult">The compiled test case result.</param>
    /// <param name="notification">The test case completion notification.</param>
    /// <returns>Performance metrics with statistical data and execution details.</returns>
    private PerformanceMetrics ExtractPerformanceMetrics(
        ICompiledTestCaseResult compiledTestCaseResult,
        TestCaseCompletedNotification notification)
    {
        var performanceResult = compiledTestCaseResult.PerformanceRunResult;

        // Handle cases where performance data might be missing due to exceptions
        if (performanceResult == null || compiledTestCaseResult.Exception != null)
        {
            logger.Log(LogLevel.Debug,
                "Performance data unavailable for test case '{0}' due to exception or missing results",
                notification.TestInstanceContainerExternal!.TestCaseId.DisplayName);

            return new PerformanceMetrics
            {
                MedianMs = 0,
                MeanMs = 0,
                StandardDeviation = 0,
                Variance = 0,
                RawExecutionResults = Array.Empty<double>(),
                DataWithOutliersRemoved = Array.Empty<double>(),
                LowerOutliers = Array.Empty<double>(),
                UpperOutliers = Array.Empty<double>(),
                TotalNumOutliers = 0,
                SampleSize = 0,
                NumWarmupIterations = 0
            };
        }

        return new PerformanceMetrics
        {
            MedianMs = performanceResult.Median,
            MeanMs = performanceResult.Mean,
            StandardDeviation = performanceResult.StdDev,
            Variance = performanceResult.Variance,
            RawExecutionResults = performanceResult.RawExecutionResults ?? Array.Empty<double>(),
            DataWithOutliersRemoved = performanceResult.DataWithOutliersRemoved ?? Array.Empty<double>(),
            LowerOutliers = performanceResult.LowerOutliers ?? Array.Empty<double>(),
            UpperOutliers = performanceResult.UpperOutliers ?? Array.Empty<double>(),
            TotalNumOutliers = (performanceResult.LowerOutliers?.Length ?? 0) + (performanceResult.UpperOutliers?.Length ?? 0),
            SampleSize = performanceResult.SampleSize,
            NumWarmupIterations = performanceResult.NumWarmupIterations
        };
    }

    #endregion

    #region Private Methods - Batching Metadata

    /// <summary>
    /// Extracts comprehensive batching metadata for cross-test-case analysis and grouping.
    /// </summary>
    /// <param name="notification">The test case completion notification.</param>
    /// <param name="currentTestCase">The current test case being processed.</param>
    /// <returns>A dictionary containing batching metadata for various grouping strategies.</returns>
    private Dictionary<string, object> ExtractBatchingMetadata(
        TestCaseCompletedNotification notification,
        TestCase currentTestCase)
    {
        var metadata = new Dictionary<string, object>();

        try
        {
            // Test class information for ByTestClass batching strategy
            var testClassName = currentTestCase.Source != null
                ? ExtractTestClassName(currentTestCase)
                : "Unknown";
            metadata["TestClassName"] = testClassName;

            // Assembly information for broader grouping
            var assemblyName = currentTestCase.Source != null
                ? System.IO.Path.GetFileNameWithoutExtension(currentTestCase.Source)
                : "Unknown";
            metadata["TestAssemblyName"] = assemblyName;

            // Test method information
            metadata["TestMethodName"] = currentTestCase.DisplayName;
            metadata["FullyQualifiedName"] = currentTestCase.FullyQualifiedName;

            // Execution context information for ByExecutionContext batching
            metadata["ExecutionContext"] = new Dictionary<string, object>
            {
                ["DisableAnalysisGlobally"] = runSettings.DisableAnalysisGlobally,
                ["RunScaleFish"] = runSettings.RunScaleFish,
                ["RunSailDiff"] = runSettings.RunSailDiff,
                ["TestExecutionDateTime"] = DateTime.UtcNow
            };

            // Performance profile information for ByPerformanceProfile batching
            var performanceProfile = ExtractPerformanceProfile(notification);
            if (performanceProfile != null)
            {
                metadata["PerformanceProfile"] = performanceProfile;
            }

            // Comparison group information for ByComparisonAttribute batching
            var comparisonGroupId = ExtractComparisonGroupId(currentTestCase);
            if (!string.IsNullOrEmpty(comparisonGroupId))
            {
                metadata["ComparisonGroup"] = comparisonGroupId;
            }

            // Comparison role information for method comparison processing
            var comparisonRole = ExtractComparisonRole(currentTestCase);
            if (!string.IsNullOrEmpty(comparisonRole))
            {
                metadata["ComparisonRole"] = comparisonRole;
            }

            // Custom criteria for ByCustomCriteria batching
            var customCriteria = ExtractCustomBatchingCriteria(currentTestCase);
            if (customCriteria.Count > 0)
            {
                metadata["CustomBatchingCriteria"] = customCriteria;
            }

            logger.Log(LogLevel.Debug,
                "Extracted {0} batching metadata entries for test case '{1}'",
                metadata.Count, currentTestCase.DisplayName);
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Warning, ex,
                "Failed to extract some batching metadata for test case '{0}': {1}",
                currentTestCase.DisplayName, ex.Message);

            // Ensure we have at least basic metadata even if extraction fails
            metadata["TestClassName"] = metadata.GetValueOrDefault("TestClassName", "Unknown");
            metadata["TestAssemblyName"] = metadata.GetValueOrDefault("TestAssemblyName", "Unknown");
        }

        return metadata;
    }

    /// <summary>
    /// Extracts the test class name from the test case.
    /// </summary>
    /// <param name="testCase">The test case to extract class name from.</param>
    /// <returns>The test class name or "Unknown" if extraction fails.</returns>
    private string ExtractTestClassName(TestCase testCase)
    {
        try
        {
            // Extract class name from fully qualified name
            var fullyQualifiedName = testCase.FullyQualifiedName;
            var lastDotIndex = fullyQualifiedName.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                var classAndMethod = fullyQualifiedName.Substring(0, lastDotIndex);
                var secondLastDotIndex = classAndMethod.LastIndexOf('.');
                return secondLastDotIndex > 0
                    ? classAndMethod.Substring(secondLastDotIndex + 1)
                    : classAndMethod;
            }

            return "Unknown";
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Debug, ex,
                "Failed to extract test class name from test case '{0}': {1}",
                testCase.DisplayName, ex.Message);
            return "Unknown";
        }
    }

    /// <summary>
    /// Extracts performance profile information for performance-based batching.
    /// </summary>
    /// <param name="notification">The test case completion notification.</param>
    /// <returns>Performance profile data or null if not available.</returns>
    private Dictionary<string, object>? ExtractPerformanceProfile(TestCaseCompletedNotification notification)
    {
        try
        {
            var compiledResult = notification.ClassExecutionSummaryTrackingFormat.ToSummaryFormat().CompiledTestCaseResults.Single();
            if (compiledResult.PerformanceRunResult == null)
                return null;

            return new Dictionary<string, object>
            {
                ["MedianMs"] = compiledResult.PerformanceRunResult.Median,
                ["MeanMs"] = compiledResult.PerformanceRunResult.Mean,
                ["SampleSize"] = compiledResult.PerformanceRunResult.SampleSize,
                ["HasOutliers"] = compiledResult.PerformanceRunResult.LowerOutliers.Length +
                                compiledResult.PerformanceRunResult.UpperOutliers.Length > 0
            };
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Debug, ex,
                "Failed to extract performance profile: {0}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Extracts comparison group identifier for attribute-based batching.
    /// </summary>
    /// <param name="testCase">The test case to extract comparison group from.</param>
    /// <returns>Comparison group identifier or null if not found.</returns>
    private string? ExtractComparisonGroupId(TestCase testCase)
    {
        try
        {
            // Extract comparison group from Sailfish test case properties
            var comparisonGroup = testCase.GetPropertyValue<string>(SailfishManagedProperty.SailfishComparisonGroupProperty, null);
            return comparisonGroup;
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Debug, ex,
                "Failed to extract comparison group ID: {0}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Extracts comparison role from a test case's properties.
    /// </summary>
    /// <param name="testCase">The test case to extract comparison role from.</param>
    /// <returns>Comparison role or null if not found.</returns>
    private string? ExtractComparisonRole(TestCase testCase)
    {
        try
        {
            // Extract comparison role from Sailfish test case properties
            var comparisonRole = testCase.GetPropertyValue<string>(SailfishManagedProperty.SailfishComparisonRoleProperty, null);
            return comparisonRole;
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Debug, ex,
                "Failed to extract comparison role: {0}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Extracts custom batching criteria from test case metadata.
    /// </summary>
    /// <param name="testCase">The test case to extract custom criteria from.</param>
    /// <returns>Dictionary of custom batching criteria.</returns>
    private Dictionary<string, object> ExtractCustomBatchingCriteria(TestCase testCase)
    {
        var criteria = new Dictionary<string, object>();

        try
        {
            // Extract custom criteria from test case traits and properties
            foreach (var trait in testCase.Traits)
            {
                if (trait.Name.StartsWith("Batch", StringComparison.OrdinalIgnoreCase) ||
                    trait.Name.StartsWith("Group", StringComparison.OrdinalIgnoreCase) ||
                    trait.Name.StartsWith("Category", StringComparison.OrdinalIgnoreCase))
                {
                    criteria[trait.Name] = trait.Value;
                }
            }

            // Could also extract from other test case metadata sources
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Debug, ex,
                "Failed to extract custom batching criteria: {0}", ex.Message);
        }

        return criteria;
    }

    #endregion

    #region Private Methods - Queue Message Creation

    /// <summary>
    /// Creates the comprehensive TestCompletionQueueMessage with all extracted data.
    /// </summary>
    /// <param name="notification">The original test case completion notification.</param>
    /// <param name="coreData">Core test execution data.</param>
    /// <param name="formattedMessage">Formatted test output message with SailDiff analysis.</param>
    /// <param name="executionResult">Test execution result data.</param>
    /// <param name="performanceMetrics">Performance metrics and statistical data.</param>
    /// <param name="batchingMetadata">Batching metadata for cross-test-case analysis.</param>
    /// <returns>A comprehensive TestCompletionQueueMessage ready for queue processing.</returns>
    private TestCompletionQueueMessage CreateQueueMessage(
        TestCaseCompletedNotification notification,
        CoreTestData coreData,
        string formattedMessage,
        ExecutionResultData executionResult,
        PerformanceMetrics performanceMetrics,
        Dictionary<string, object> batchingMetadata)
    {
        var performanceTimer = notification.TestInstanceContainerExternal!.PerformanceTimer!;

        // Create comprehensive metadata dictionary
        var metadata = new Dictionary<string, object>
        {
            // Core data required for framework publishing
            ["TestCase"] = coreData.CurrentTestCase,
            ["FormattedMessage"] = formattedMessage,
            ["StartTime"] = performanceTimer.GetIterationStartTime(),
            ["EndTime"] = performanceTimer.GetIterationStopTime(),
            ["MedianRuntime"] = executionResult.MedianRuntime,
            ["StatusCode"] = executionResult.StatusCode,

            // Exception data (if any)
            ["Exception"] = executionResult.Exception as object ?? DBNull.Value,

            // Additional context for processors
            ["ClassExecutionSummaries"] = coreData.ClassExecutionSummaries,
            ["CompiledTestCaseResult"] = coreData.CompiledTestCaseResult,
            ["TestCaseGroup"] = notification.TestCaseGroup,
            ["RunSettings"] = runSettings,

            // Notification context
            ["OriginalNotification"] = notification
        };

        // Merge batching metadata
        foreach (var kvp in batchingMetadata)
        {
            metadata[$"Batching_{kvp.Key}"] = kvp.Value;
        }

        var queueMessage = new TestCompletionQueueMessage
        {
            TestCaseId = notification.TestInstanceContainerExternal.TestCaseId.DisplayName,
            CompletedAt = DateTime.UtcNow,
            TestResult = new TestExecutionResult
            {
                IsSuccess = executionResult.IsSuccess,
                ExceptionMessage = executionResult.Exception?.Message,
                ExceptionDetails = executionResult.Exception?.ToString(),
                ExceptionType = executionResult.Exception?.GetType().Name
            },
            PerformanceMetrics = performanceMetrics,
            Metadata = metadata
        };

        return queueMessage;
    }

    #endregion

    #region Private Helper Classes

    /// <summary>
    /// Contains core test execution data extracted from the notification.
    /// </summary>
    private class CoreTestData
    {
        public IClassExecutionSummary ClassExecutionSummaries { get; set; } = null!;
        public string TestOutputWindowMessage { get; set; } = string.Empty;
        public TestCase CurrentTestCase { get; set; } = null!;
        public ICompiledTestCaseResult CompiledTestCaseResult { get; set; } = null!;
    }

    /// <summary>
    /// Contains test execution result information.
    /// </summary>
    private class ExecutionResultData
    {
        public bool IsSuccess { get; set; }
        public Exception? Exception { get; set; }
        public StatusCode StatusCode { get; set; }
        public double MedianRuntime { get; set; }
    }

    #endregion
}
