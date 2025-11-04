using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Processors.MethodComparison;

/// <summary>
/// Batch processor for handling method comparisons across multiple test cases.
/// This processor analyzes complete batches to identify comparison pairs and
/// perform SailDiff analysis between Before and After methods.
/// </summary>
/// <remarks>
/// This processor should be registered to handle batches rather than individual messages.
/// It will be called when a batch is complete and can analyze all test cases in the batch
/// to identify comparison groups and perform the actual SailDiff comparisons.
///
/// The batch processing approach allows us to:
/// - Detect when both Before and After methods in a comparison group have completed
/// - Perform comparisons only when full test classes are being executed
/// - Generate enhanced output that includes comparison results
/// - Maintain proper ordering and timing of result publication
/// </remarks>
internal class MethodComparisonBatchProcessor
{
    private readonly IAdapterSailDiff _sailDiff;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly ISailDiffUnifiedFormatter _unifiedFormatter;

    public MethodComparisonBatchProcessor(
        IAdapterSailDiff sailDiff,
        IMediator mediator,
        ILogger logger,
        ISailDiffUnifiedFormatter unifiedFormatter)
    {
        _sailDiff = sailDiff ?? throw new ArgumentNullException(nameof(sailDiff));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unifiedFormatter = unifiedFormatter ?? throw new ArgumentNullException(nameof(unifiedFormatter));
    }

    /// <summary>
    /// Processes a batch of test completion messages to perform method comparisons.
    /// </summary>
    /// <param name="batch">The batch of test completion messages.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    public async Task ProcessBatch(TestCaseBatch batch, CancellationToken cancellationToken)
    {
        _logger.Log(LogLevel.Information,
            "MethodComparisonBatchProcessor: Processing batch with {0} test cases",
            batch?.TestCases.Count ?? 0);

        if (batch == null || batch.TestCases.Count == 0)
        {
            _logger.Log(LogLevel.Warning, "Batch is null or empty - no comparison processing will occur");
            return;
        }

        // Log all test cases in the batch for debugging
        foreach (var testCase in batch.TestCases)
        {
            var group = ExtractComparisonGroup(testCase);
            var role = ExtractComparisonRole(testCase);
            _logger.Log(LogLevel.Debug,
                "Batch contains test case '{0}' - Group: '{1}', Role: '{2}'",
                testCase.TestCaseId, group ?? "null", role ?? "null");
        }

        // Group test cases by comparison group
        var comparisonGroups = batch.TestCases
            .Where(HasComparisonMetadata)
            .GroupBy(ExtractComparisonGroup)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToList();

        // Process comparisons if we have complete comparison groups, regardless of full class execution
        // This allows comparisons to work when running individual comparison methods
        _logger.Log(LogLevel.Information,
            "Found {0} comparison groups in batch. Checking for complete groups...", comparisonGroups.Count);

        foreach (var group in comparisonGroups)
        {
            var groupMethods = group.ToList();

            if (groupMethods.Count >= 2)
            {
                _logger.Log(LogLevel.Information,
                    "Processing comparison group '{0}' with {1} methods",
                    group.Key, groupMethods.Count);
                await ProcessComparisonGroup(group.Key!, groupMethods, cancellationToken);
            }
            else
            {
                _logger.Log(LogLevel.Debug,
                    "Skipping comparison group '{0}' with insufficient methods: {1}",
                    group.Key, groupMethods.Count);
            }
        }
    }

    /// <summary>
    /// Processes a single comparison group to perform SailDiff analysis.
    /// </summary>
    /// <param name="groupName">The name of the comparison group.</param>
    /// <param name="testCases">The test cases in the comparison group.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    private async Task ProcessComparisonGroup(string groupName, List<TestCompletionQueueMessage> testCases,
        CancellationToken cancellationToken)
    {
        if (testCases.Count < 2)
        {
            _logger.Log(LogLevel.Warning,
                "Insufficient methods in comparison group '{0}': found {1} methods",
                groupName, testCases.Count);
            return;
        }

        // Note: Already logged in ProcessBatch, no need to log again here

        // For true N×N comparison, each method needs to be compared with every other method
        // We need to ensure each method gets its own perspective on all other methods
        var allPairs = new HashSet<(string, string)>();

        // Generate all unique pairs for SailDiff comparison (avoid duplicate SailDiff calls)
        for (int i = 0; i < testCases.Count; i++)
        {
            for (int j = i + 1; j < testCases.Count; j++)
            {
                var methodA = testCases[i];
                var methodB = testCases[j];
                var pairKey = (methodA.TestCaseId, methodB.TestCaseId);

                if (allPairs.Add(pairKey))
                {
                    // Perform comparison between the two methods (this will generate perspective-specific output for both)
                    await PerformMethodComparison(methodA, methodB, groupName, cancellationToken);
                }
            }
        }

        // After all comparisons are complete, remove suppression flags and republish all methods in the group
        foreach (var testCase in testCases)
        {
            testCase.Metadata.Remove("SuppressIndividualOutput");
        }

        // Republish all enhanced FrameworkTestCaseEndNotification messages
        await PublishEnhancedFrameworkNotificationsForGroup(testCases, cancellationToken);
    }

    private static bool HasComparisonMetadata(TestCompletionQueueMessage message)
    {
        return message.Metadata.ContainsKey("ComparisonGroup");
    }

    private string? ExtractComparisonGroup(TestCompletionQueueMessage message)
    {
        return message.Metadata.TryGetValue("ComparisonGroup", out var group) ? group?.ToString() : null;
    }

    private string? ExtractComparisonRole(TestCompletionQueueMessage message)
    {
        return message.Metadata.TryGetValue("ComparisonRole", out var role) ? role?.ToString() : null;
    }

    /// <summary>
    /// Performs SailDiff comparison between a Before and After method.
    /// </summary>
    private async Task PerformMethodComparison(
        TestCompletionQueueMessage beforeMethod,
        TestCompletionQueueMessage afterMethod,
        string groupName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract performance data from messages
            var beforeData = CreatePerformanceRunResultFromMessage(beforeMethod);
            var afterData = CreatePerformanceRunResultFromMessage(afterMethod);

            // Create combined class execution summary from both methods
            var classExecutionSummary = CreateCombinedClassExecutionSummary(beforeMethod, afterMethod);

            // Debug: Log class execution summary contents
            _logger.Log(LogLevel.Debug,
                "Class execution summary contains {0} compiled test case results",
                classExecutionSummary.CompiledTestCaseResults.Count());

            foreach (var result in classExecutionSummary.CompiledTestCaseResults)
            {
                _logger.Log(LogLevel.Debug,
                    "Compiled result: DisplayName='{0}', HasPerformanceResult={1}",
                    result.PerformanceRunResult?.DisplayName ?? "null", result.PerformanceRunResult != null);
            }

            // Perform SailDiff comparison using the comparison group name as a common test case ID
            // This allows SailDiff to compare different methods by treating them as before/after versions
            var commonTestCaseId = $"Comparison_{groupName}";

            _logger.Log(LogLevel.Debug,
                "Calling SailDiff.ComputeTestCaseDiff for group '{0}' with before: '{1}', after: '{2}', using common ID: '{3}'",
                groupName, beforeMethod.TestCaseId, afterMethod.TestCaseId, commonTestCaseId);

            var comparisonResult = _sailDiff.ComputeTestCaseDiff(
                [commonTestCaseId],
                [commonTestCaseId],
                commonTestCaseId,
                CreateModifiedClassExecutionSummary(classExecutionSummary, beforeMethod, afterMethod, commonTestCaseId),
                CreateModifiedPerformanceResult(beforeData, commonTestCaseId));

            _logger.Log(LogLevel.Debug,
                "SailDiff comparison completed for group '{0}'. Results count: {1}",
                groupName, comparisonResult?.SailDiffResults?.Count ?? 0);

            // Format comparison results from each method's perspective
            var beforeMethodOutput = FormatComparisonResults(comparisonResult, groupName, beforeMethod.TestCaseId,
                afterMethod.TestCaseId, beforeMethod.TestCaseId);
            var afterMethodOutput = FormatComparisonResults(comparisonResult, groupName, beforeMethod.TestCaseId,
                afterMethod.TestCaseId, afterMethod.TestCaseId);

            // Enhance test output messages with perspective-specific results
            EnhanceTestOutputWithComparison(beforeMethod, afterMethod, beforeMethodOutput, afterMethodOutput,
                cancellationToken);

            // Note: We don't remove suppression flags or republish here
            // This will be done after ALL comparisons in the group are complete

            _logger.Log(LogLevel.Information,
                "Completed comparison for group '{0}': {1} vs {2}",
                groupName, beforeMethod.TestCaseId, afterMethod.TestCaseId);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to perform comparison for group '{0}': {1}",
                groupName, ex.Message);
        }
    }

    /// <summary>
    /// Creates a PerformanceRunResult from a TestCompletionQueueMessage.
    /// Computes CI fields since PerformanceMetrics does not carry them.
    /// </summary>
    private PerformanceRunResult CreatePerformanceRunResultFromMessage(TestCompletionQueueMessage message)
    {
        var metrics = message.PerformanceMetrics;

        // Fallback to raw samples if cleaned data is unavailable
        var clean = metrics.DataWithOutliersRemoved ?? metrics.RawExecutionResults ?? [];
        var n = clean.Length;
        var mean = metrics.MeanMs;
        var stdDev = metrics.StandardDeviation;
        var standardError = n > 1 ? stdDev / Math.Sqrt(n) : 0;

        // Default report levels when settings are not available from the message
        var reportLevels = new List<double> { 0.95, 0.99 };

        // Guard against n == 0 to prevent ArgumentOutOfRangeException
        var ciList = n > 0
            ? PerformanceRunResult.ComputeConfidenceIntervals(mean, standardError, n, reportLevels)
            : [];

        // Primary (legacy) fields use 0.95 by default
        var primary = ciList.FirstOrDefault(x => Math.Abs(x.ConfidenceLevel - 0.95) < 1e-9)
                      ?? (n > 0
                          ? PerformanceRunResult.ComputeConfidenceIntervals(mean, standardError, n, [0.95]).First()
                          : new ConfidenceIntervalResult(0.95, 0, mean, mean));

        return new PerformanceRunResult(
            message.TestCaseId,
            mean,
            stdDev,
            metrics.Variance,
            metrics.MedianMs,
            metrics.RawExecutionResults ?? [],
            metrics.SampleSize,
            metrics.NumWarmupIterations,
            clean,
            metrics.UpperOutliers ?? [],
            metrics.LowerOutliers ?? [],
            metrics.TotalNumOutliers,
            standardError,
            primary.ConfidenceLevel,
            primary.Lower,
            primary.Upper,
            primary.MarginOfError,
            ciList);
    }

    /// <summary>
    /// Creates a modified class execution summary where both before and after results use the same test case ID.
    /// This allows SailDiff to compare different methods by treating them as before/after versions.
    /// </summary>
    private IClassExecutionSummary CreateModifiedClassExecutionSummary(
        IClassExecutionSummary originalSummary,
        TestCompletionQueueMessage beforeMethod,
        TestCompletionQueueMessage afterMethod,
        string commonTestCaseId)
    {
        // Find the after method's result in the original summary
        var afterResult = originalSummary.CompiledTestCaseResults
            .FirstOrDefault(x => x.PerformanceRunResult?.DisplayName == afterMethod.TestCaseId);

        if (afterResult?.PerformanceRunResult == null)
        {
            _logger.Log(LogLevel.Warning,
                "Could not find after method result for '{0}' in class execution summary",
                afterMethod.TestCaseId);
            return originalSummary;
        }

        // Create a modified performance result with the common test case ID
        var modifiedAfterResult = CreateModifiedPerformanceResult(afterResult.PerformanceRunResult, commonTestCaseId);
        var modifiedCompiledResult = new ModifiedCompiledTestCaseResult(afterResult, modifiedAfterResult);

        return new CombinedClassExecutionSummary(
            originalSummary.TestClass,
            originalSummary.ExecutionSettings,
            [modifiedCompiledResult]);
    }

    /// <summary>
    /// Creates a modified PerformanceRunResult with a different display name.
    /// This allows SailDiff to compare different methods using a common test case ID.
    /// </summary>
    private PerformanceRunResult CreateModifiedPerformanceResult(PerformanceRunResult original, string newDisplayName)
    {
        return new PerformanceRunResult(
            newDisplayName,
            original.Mean,
            original.StdDev,
            original.Variance,
            original.Median,
            original.RawExecutionResults,
            original.SampleSize,
            original.NumWarmupIterations,
            original.DataWithOutliersRemoved,
            original.UpperOutliers,
            original.LowerOutliers,
            original.TotalNumOutliers,
            original.StandardError,
            original.ConfidenceLevel,
            original.ConfidenceIntervalLower,
            original.ConfidenceIntervalUpper,
            original.MarginOfError);
    }

    /// <summary>
    /// Creates a combined class execution summary from both before and after method messages.
    /// This ensures SailDiff has access to both test results for comparison.
    /// </summary>
    private IClassExecutionSummary CreateCombinedClassExecutionSummary(
        TestCompletionQueueMessage beforeMethod,
        TestCompletionQueueMessage afterMethod)
    {
        var beforeSummary = ExtractClassExecutionSummary(beforeMethod);
        var afterSummary = ExtractClassExecutionSummary(afterMethod);

        // Combine the compiled test case results from both summaries
        var combinedResults = beforeSummary.CompiledTestCaseResults
            .Concat(afterSummary.CompiledTestCaseResults)
            .ToList();

        _logger.Log(LogLevel.Debug,
            "Created combined class execution summary with {0} results (before: {1}, after: {2})",
            combinedResults.Count, beforeSummary.CompiledTestCaseResults.Count(),
            afterSummary.CompiledTestCaseResults.Count());

        // Create a new combined summary using the before summary as the base
        return new CombinedClassExecutionSummary(
            beforeSummary.TestClass,
            beforeSummary.ExecutionSettings,
            combinedResults);
    }

    /// <summary>
    /// Extracts class execution summary from message metadata.
    /// </summary>
    private IClassExecutionSummary ExtractClassExecutionSummary(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue("ClassExecutionSummaries", out var summariesObj))
        {
            // Handle both single IClassExecutionSummary and IEnumerable<IClassExecutionSummary> cases
            if (summariesObj is IClassExecutionSummary singleSummary)
            {
                return singleSummary;
            }

            if (summariesObj is IEnumerable<IClassExecutionSummary> summaries)
            {
                return summaries.FirstOrDefault() ??
                       throw new InvalidOperationException("No class execution summary found in collection");
            }
        }

        throw new InvalidOperationException("Class execution summary not found in message metadata");
    }

    /// <summary>
    /// Formats SailDiff comparison results for display from a specific method's perspective using the unified formatter.
    /// </summary>
    private string FormatComparisonResults(TestCaseSailDiffResult comparisonResult, string groupName,
        string beforeMethodName, string afterMethodName, string perspectiveMethodName)
    {
        _logger.Log(LogLevel.Debug,
            "Formatting comparison results for group '{0}' from perspective '{1}'. ComparisonResult is null: {2}, SailDiffResults count: {3}",
            groupName, ExtractMethodName(perspectiveMethodName), comparisonResult == null,
            comparisonResult?.SailDiffResults?.Count() ?? 0);

        if (comparisonResult?.SailDiffResults?.Any() != true)
        {
            return "\n❌ No comparison results available\n";
        }

        // Convert SailDiff result to unified comparison data
        var result = comparisonResult.SailDiffResults.First();
        var isBeforePerspective = perspectiveMethodName == beforeMethodName;
        var primaryMethod = ExtractMethodName(perspectiveMethodName);
        var comparedMethod = ExtractMethodName(isBeforePerspective ? afterMethodName : beforeMethodName);

        var comparisonData = new SailDiffComparisonData
        {
            GroupName = groupName,
            PrimaryMethodName = primaryMethod,
            ComparedMethodName = comparedMethod,
            Statistics = result.TestResultsWithOutlierAnalysis.StatisticalTestResult,
            Metadata = new ComparisonMetadata
            {
                SampleSize = result.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore,
                AlphaLevel = comparisonResult.TestSettings.Alpha,
                TestType = GetTestTypeDisplayName(comparisonResult.TestSettings.TestType),
                OutliersRemoved = (result.TestResultsWithOutlierAnalysis.Sample1?.TotalNumOutliers ?? 0) +
                                  (result.TestResultsWithOutlierAnalysis.Sample2?.TotalNumOutliers ?? 0)
            },
            IsPerspectiveBased = true,
            PerspectiveMethodName = perspectiveMethodName
        };

        // Format using unified formatter for IDE context
        var formattedOutput = _unifiedFormatter.Format(comparisonData, OutputContext.IDE);

        _logger.Log(LogLevel.Debug,
            "Unified formatter generated output for '{0}' vs '{1}'. Significance: {2}, Change: {3:F1}%",
            primaryMethod, comparedMethod, formattedOutput.Significance, formattedOutput.PercentageChange);

        return formattedOutput.FullOutput;
    }

    /// <summary>
    /// Converts TestType enum to user-friendly display name
    /// </summary>
    private static string GetTestTypeDisplayName(TestType testType)
    {
        return testType switch
        {
            TestType.TwoSampleWilcoxonSignedRankTest => "Two-Sample Wilcoxon Signed-Rank Test",
            TestType.WilcoxonRankSumTest => "Wilcoxon Rank-Sum Test",
            TestType.Test => "T-Test",
            TestType.KolmogorovSmirnovTest => "Kolmogorov-Smirnov Test",
            _ => testType.ToString()
        };
    }

    /// <summary>
    /// Extracts the method name from a full test case ID.
    /// </summary>
    private static string ExtractMethodName(string testCaseId)
    {
        // Extract method name from test case ID (e.g., "ClassName.MethodName" -> "MethodName")
        var lastDotIndex = testCaseId.LastIndexOf('.');
        return lastDotIndex >= 0 ? testCaseId.Substring(lastDotIndex + 1) : testCaseId;
    }

    /// <summary>
    /// Enhances test output messages with comparison results from each method's perspective.
    /// Accumulates multiple comparison results for methods that are compared with multiple other methods.
    /// </summary>
    private void EnhanceTestOutputWithComparison(
        TestCompletionQueueMessage beforeMethod,
        TestCompletionQueueMessage afterMethod,
        string beforeMethodOutput,
        string afterMethodOutput,
        CancellationToken cancellationToken)
    {
        // Accumulate comparison results for the "before" method
        AccumulateComparisonOutput(beforeMethod, beforeMethodOutput);

        // Accumulate comparison results for the "after" method
        AccumulateComparisonOutput(afterMethod, afterMethodOutput);
    }

    /// <summary>
    /// Accumulates comparison output for a method, preserving existing comparisons.
    /// </summary>
    private void AccumulateComparisonOutput(TestCompletionQueueMessage method, string newComparisonOutput)
    {
        const string ComparisonResultsKey = "AccumulatedComparisons";

        // Get existing accumulated comparisons
        if (method.Metadata.TryGetValue(ComparisonResultsKey, out var existingObj) &&
            existingObj is List<string> existingComparisons)
        {
            // Add the new comparison to the list
            existingComparisons.Add(newComparisonOutput);
            _logger.Log(LogLevel.Debug,
                "Added comparison to existing list for '{0}'. Total comparisons: {1}",
                method.TestCaseId, existingComparisons.Count);
        }
        else
        {
            // Create new list with the first comparison
            method.Metadata[ComparisonResultsKey] = new List<string> { newComparisonOutput };
            _logger.Log(LogLevel.Debug,
                "Created new comparison list for '{0}' with first comparison",
                method.TestCaseId);
        }

        // Update the formatted message with all accumulated comparisons
        var allComparisons = (List<string>)method.Metadata[ComparisonResultsKey];
        var combinedOutput = string.Join("", allComparisons);

        // Check if there's any original non-comparison content to preserve
        string originalContent = "";
        if (method.Metadata.TryGetValue("OriginalFormattedMessage", out var originalObj) &&
            originalObj is string original)
        {
            originalContent = original;
        }
        else if (method.Metadata.TryGetValue("FormattedMessage", out var existingMessageObj) &&
                 existingMessageObj is string existingMessage &&
                 !existingMessage.Contains("📊 COMPARISON RESULTS:"))
        {
            // Store the original message before any comparisons were added
            originalContent = existingMessage;
            method.Metadata["OriginalFormattedMessage"] = originalContent;
        }

        // Always set the formatted message to original content + all comparisons
        method.Metadata["FormattedMessage"] = originalContent + combinedOutput;

        _logger.Log(LogLevel.Debug,
            "Updated FormattedMessage for '{0}' with {1} accumulated comparisons. Combined length: {2}",
            method.TestCaseId, allComparisons.Count, combinedOutput.Length);
    }

    /// <summary>
    /// Publishes enhanced framework notifications for all methods in a comparison group.
    /// </summary>
    private async Task PublishEnhancedFrameworkNotificationsForGroup(
        List<TestCompletionQueueMessage> testCases,
        CancellationToken cancellationToken)
    {
        foreach (var testCase in testCases)
        {
            var notification = CreateFrameworkNotification(testCase);
            await _mediator.Publish(notification, cancellationToken);

            _logger.Log(LogLevel.Information,
                "Published enhanced framework notification for '{0}' with accumulated comparisons",
                testCase.TestCaseId);
        }
    }

    /// <summary>
    /// Creates a FrameworkTestCaseEndNotification from a TestCompletionQueueMessage.
    /// </summary>
    private FrameworkTestCaseEndNotification CreateFrameworkNotification(TestCompletionQueueMessage message)
    {
        var enhancedMessage = message.Metadata.TryGetValue("FormattedMessage", out var msgObj)
            ? msgObj?.ToString() ?? string.Empty
            : string.Empty;

        // Use the original TestCase from metadata to ensure framework compatibility
        var testCase = message.Metadata.TryGetValue("TestCase", out var testCaseObj) &&
                       testCaseObj is TestCase originalTestCase
            ? originalTestCase
            : throw new InvalidOperationException(
                $"Original TestCase not found in metadata for test case '{message.TestCaseId}'");

        var startTime = message.Metadata.TryGetValue("StartTime", out var startTimeObj) &&
                        startTimeObj is DateTimeOffset start
            ? start
            : message.CompletedAt;

        var endTime = message.Metadata.TryGetValue("EndTime", out var endTimeObj) && endTimeObj is DateTimeOffset end
            ? end
            : message.CompletedAt;

        var medianRuntime = message.PerformanceMetrics.MedianMs;

        var statusCode = message.TestResult.IsSuccess ? StatusCode.Success : StatusCode.Failure;

        // Create exception from TestExecutionResult if test failed
        Exception? exception = null;
        if (!message.TestResult.IsSuccess && !string.IsNullOrEmpty(message.TestResult.ExceptionMessage))
        {
            exception = new Exception(message.TestResult.ExceptionMessage);
        }

        return new FrameworkTestCaseEndNotification(
            enhancedMessage,
            startTime,
            endTime,
            medianRuntime,
            testCase,
            statusCode,
            exception);
    }
}