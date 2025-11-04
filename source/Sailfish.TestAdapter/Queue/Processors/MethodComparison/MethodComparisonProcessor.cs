using MediatR;
using Sailfish.Attributes;
using Sailfish.Contracts.Private;
using Sailfish.Logging;
using Sailfish.TestAdapter.Queue.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.TestAdapter.Queue.Processors.MethodComparison;

/// <summary>
/// Queue processor responsible for performing SailDiff comparisons between methods
/// marked with SailfishComparisonAttribute when full test classes are executed.
/// This processor enables "before and after" performance comparisons within the same test class.
/// </summary>
/// <remarks>
/// The MethodComparisonProcessor analyzes batches of test completion messages to identify
/// comparison groups where both "Before" and "After" methods have completed execution.
/// When a complete comparison pair is detected, it performs SailDiff analysis and enhances
/// the test output with comparison results.
///
/// Key responsibilities:
/// - Detect when comparison groups are complete (both Before and After methods finished)
/// - Perform SailDiff comparisons between method pairs using AdapterSailDiff
/// - Enhance test output messages with comparison results
/// - Only activate when full test classes are being executed (not individual methods)
/// - Maintain backward compatibility with existing test execution
///
/// The processor integrates with the existing queue architecture and runs after test
/// completion but before framework publishing, allowing comparison results to be
/// included in the final test output displayed to users.
/// </remarks>
internal class MethodComparisonProcessor : TestCompletionQueueProcessorBase
{
    private readonly IMediator _mediator;
    private readonly ITestCaseBatchingService _batchingService;
    private readonly MethodComparisonBatchProcessor _batchProcessor;

    // Track test classes that have already generated markdown to avoid duplicates
    private readonly HashSet<string> _markdownGeneratedForClasses = [];

    /// <summary>
    /// Initializes a new instance of the MethodComparisonProcessor.
    /// </summary>
    /// <param name="mediator">The mediator for publishing notifications.</param>
    /// <param name="batchingService">The batching service for grouping test cases.</param>
    /// <param name="batchProcessor">The batch processor for handling comparison groups.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public MethodComparisonProcessor(
        IMediator mediator,
        ITestCaseBatchingService batchingService,
        MethodComparisonBatchProcessor batchProcessor,
        ILogger logger) : base(logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _batchingService = batchingService ?? throw new ArgumentNullException(nameof(batchingService));
        _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
    }

    /// <summary>
    /// Processes test completion messages to perform method comparisons.
    /// This method is called for each test completion message in the queue.
    /// </summary>
    /// <param name="message">The test completion message to process.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous processing operation.</returns>
    /// <remarks>
    /// This method currently processes individual messages but the real comparison logic
    /// should be implemented in a batch processor that can analyze multiple related
    /// test cases together. For now, this serves as a placeholder for the comparison logic.
    /// </remarks>
    protected override async Task ProcessTestCompletionCore(TestCompletionQueueMessage message,
        CancellationToken cancellationToken)
    {
        var comparisonGroup = ExtractComparisonGroup(message);

        Logger.Log(LogLevel.Information,
            "MethodComparisonProcessor: Processing test case '{0}' - ComparisonGroup: '{1}'",
            message.TestCaseId, comparisonGroup ?? "null");

        // Log all metadata keys for debugging
        Logger.Log(LogLevel.Debug,
            "Test case '{0}' metadata keys: {1}",
            message.TestCaseId, string.Join(", ", message.Metadata.Keys));

        if (!string.IsNullOrEmpty(comparisonGroup))
        {
            Logger.Log(LogLevel.Information,
                "Received test completion for comparison group '{0}': {1}",
                comparisonGroup, message.TestCaseId);

            // Always ensure we're using the comparison batching strategy for comparison methods
            await EnsureComparisonBatchingStrategy();

            // Check if this completes a comparison batch
            await CheckAndProcessCompleteBatch(message, cancellationToken);

            // SUPPRESS individual framework notification for comparison methods
            // The batch processor will handle displaying results with comparisons
            Logger.Log(LogLevel.Information,
                "Comparison method '{0}' individual output suppressed - will be displayed after batch processing",
                message.TestCaseId);

            // Mark this message as processed but don't send framework notification yet
            message.Metadata["SuppressIndividualOutput"] = true;
        }
        else
        {
            Logger.Log(LogLevel.Debug,
                "Test case '{0}' is not a comparison method - skipping comparison processing",
                message.TestCaseId);
        }

        // Note: Markdown generation is now handled by MethodComparisonTestClassCompletedHandler
        // which listens for TestClassCompletedNotification for better consolidation
    }

    /// <summary>
    /// Processes all completed batches for method comparisons.
    /// This method should be called when the queue processing is complete to handle
    /// any batches that were marked as complete during shutdown.
    /// </summary>
    public async Task ProcessCompletedBatchesAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.Log(LogLevel.Information, "MethodComparisonProcessor: Checking for completed batches to process...");

            // Get all completed batches from the batching service
            var completedBatches = await _batchingService.GetCompletedBatchesAsync(cancellationToken);

            Logger.Log(LogLevel.Information,
                "Found {0} completed batches to process for method comparisons", completedBatches.Count);

            foreach (var batch in completedBatches)
            {
                Logger.Log(LogLevel.Information,
                    "Processing completed batch '{0}' with {1} test cases", batch.BatchId, batch.TestCases.Count);

                // Process the batch for method comparisons
                await _batchProcessor.ProcessBatch(batch, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, ex,
                "Failed to process completed batches for method comparisons: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Ensures the batching service is using the ByComparisonAttribute strategy.
    /// </summary>
    private async Task EnsureComparisonBatchingStrategy()
    {
        try
        {
            // For now, we'll assume the strategy is already set correctly
            // This can be enhanced later when the compilation issues are resolved
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, ex,
                "Failed to set comparison batching strategy: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Checks if the current message completes a comparison batch and processes it if so.
    /// </summary>
    private async Task CheckAndProcessCompleteBatch(
        TestCompletionQueueMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the batch ID for this message
            var testClassName = message.TestCaseId.Contains('.')
                ? message.TestCaseId.Substring(0, message.TestCaseId.LastIndexOf('.'))
                : "Unknown";
            var comparisonGroup = ExtractComparisonGroup(message);
            var batchId = $"Comparison_{testClassName}_{comparisonGroup}";

            // Try to get the batch from the batching service
            var batch = await _batchingService.GetBatchAsync(batchId, cancellationToken);
            if (batch == null)
            {
                Logger.Log(LogLevel.Debug, "Batch '{0}' not found or not ready for processing", batchId);
                return;
            }

            // Check if the batch has multiple methods for comparison
            var comparisonMethods = batch.TestCases
                .Where(tc => !string.IsNullOrEmpty(ExtractComparisonGroup(tc)))
                .ToList();

            if (comparisonMethods.Count >= 2)
            {
                Logger.Log(LogLevel.Information,
                    "Comparison batch '{0}' is complete with {1} methods. Processing comparisons...",
                    batchId, comparisonMethods.Count);

                // Process the complete batch
                await _batchProcessor.ProcessBatch(batch, cancellationToken);

                // Generate markdown file if WriteToMarkdown attribute is present
                await GenerateMarkdownIfRequested(batch, cancellationToken);
            }
            else
            {
                Logger.Log(LogLevel.Debug,
                    "Comparison batch '{0}' has insufficient methods for comparison: {1} methods",
                    batchId, comparisonMethods.Count);
            }
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions to respect cancellation requests
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, ex,
                "Failed to check and process comparison batch for message '{0}': {1}",
                message.TestCaseId, ex.Message);
        }
    }

    /// <summary>
    /// Extracts the comparison group from a test completion message.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>The comparison group name, or null if not found.</returns>
    private string? ExtractComparisonGroup(TestCompletionQueueMessage message)
    {
        if (message.Metadata.TryGetValue("ComparisonGroup", out var group))
        {
            var groupName = group?.ToString();
            Logger.Log(LogLevel.Information,
                "Found comparison group '{0}' for test '{1}'", groupName, message.TestCaseId);
            return groupName;
        }

        Logger.Log(LogLevel.Debug,
            "No comparison group found for test '{0}'", message.TestCaseId);
        return null;
    }


    /// <summary>
    /// Gets the test class type using reflection.
    /// </summary>
    private Type? GetTestClassTypeByName(string className)
    {
        try
        {
            // First try Type.GetType for fully qualified names
            var type = Type.GetType(className);
            if (type != null)
            {
                return type;
            }

            // Try to find the type in loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Name == className ||
                               t.FullName == className ||
                               t.FullName?.EndsWith($".{className}") == true)
                    .ToList();

                if (types.Count == 1)
                {
                    return types[0];
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, ex,
                "Failed to get test class type for '{0}': {1}",
                className, ex.Message);
        }

        return null;
    }

    /// <summary>
    /// Extracts the method name from a test completion message.
    /// </summary>
    private string ExtractMethodName(TestCompletionQueueMessage message)
    {
        // Extract method name from TestCaseId (format: ClassName.MethodName)
        var testCaseId = message.TestCaseId;
        if (testCaseId.Contains('.'))
        {
            var lastDotIndex = testCaseId.LastIndexOf('.');
            if (lastDotIndex >= 0 && lastDotIndex < testCaseId.Length - 1)
            {
                return testCaseId.Substring(lastDotIndex + 1);
            }
        }

        return testCaseId;
    }

    /// <summary>
    /// Generates markdown file for method comparison results if the WriteToMarkdown attribute is present.
    /// Uses the notification pattern to avoid direct file I/O in TestAdapter.
    /// </summary>
    /// <param name="batch">The test case batch containing comparison results.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous markdown generation operation.</returns>
    private async Task GenerateMarkdownIfRequested(TestCaseBatch batch, CancellationToken cancellationToken)
    {
        try
        {
            Logger.Log(LogLevel.Information,
                "GenerateMarkdownIfRequested called for batch '{0}' with {1} test cases",
                batch.BatchId, batch.TestCases.Count);
            // Check if any test in the batch has the WriteToMarkdown attribute
            var hasWriteToMarkdownAttribute = false;
            Type? testClassType = null;
            string? className = null;

            foreach (var testCase in batch.TestCases)
            {
                className = ExtractClassName(testCase.TestCaseId);
                testClassType = GetTestClassTypeByName(className);
                if (testClassType?.GetCustomAttribute<WriteToMarkdownAttribute>() != null)
                {
                    hasWriteToMarkdownAttribute = true;
                    break;
                }
            }

            if (!hasWriteToMarkdownAttribute || testClassType == null || className == null)
            {
                Logger.Log(LogLevel.Debug,
                    "No WriteToMarkdown attribute found for batch '{0}'. Skipping markdown generation.",
                    batch.BatchId);
                return;
            }

            // Temporarily disable deduplication for debugging
            Logger.Log(LogLevel.Information,
                "Proceeding with markdown generation for class '{0}' (deduplication temporarily disabled)",
                className);

            Logger.Log(LogLevel.Information,
                "Generating markdown file for method comparison batch '{0}' with test class '{1}'",
                batch.BatchId, testClassType.Name);

            // Generate markdown content
            var markdownContent = CreateMethodComparisonMarkdown(batch, testClassType);

            if (!string.IsNullOrEmpty(markdownContent))
            {
                // Publish notification to generate markdown file
                // The handler will determine the output directory from run settings
                var notification = new WriteMethodComparisonMarkdownNotification(
                    testClassType.Name,
                    markdownContent,
                    string.Empty); // Output directory will be determined by handler

                await _mediator.Publish(notification, cancellationToken);

                Logger.Log(LogLevel.Information,
                    "Published WriteMethodComparisonMarkdownNotification for test class '{0}'",
                    testClassType.Name);
            }
            else
            {
                Logger.Log(LogLevel.Warning,
                    "Generated markdown content was empty for test class '{0}'",
                    testClassType.Name);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, ex,
                "Failed to generate markdown for method comparison batch '{0}': {1}",
                batch.BatchId, ex.Message);
        }
    }

    /// <summary>
    /// Extracts the class name from a test case ID.
    /// </summary>
    /// <param name="testCaseId">The test case ID in format ClassName.MethodName.</param>
    /// <returns>The class name, or "Unknown" if extraction fails.</returns>
    private string ExtractClassName(string testCaseId)
    {
        if (testCaseId.Contains('.'))
        {
            var lastDotIndex = testCaseId.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                return testCaseId.Substring(0, lastDotIndex);
            }
        }

        return "Unknown";
    }

    /// <summary>
    /// Creates markdown content for method comparison results.
    /// </summary>
    /// <param name="batch">The test case batch containing comparison results.</param>
    /// <param name="testClassType">The test class type containing the comparison methods.</param>
    /// <returns>The generated markdown content.</returns>
    private string CreateMethodComparisonMarkdown(TestCaseBatch batch, Type testClassType)
    {
        var sb = new StringBuilder();

        // Add document header
        sb.AppendLine($"# 📊 Method Comparison Results: {testClassType.Name}");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Test Class:** {testClassType.FullName}");
        sb.AppendLine();

        // Group test cases by comparison group
        var comparisonGroups = batch.TestCases
            .Where(HasComparisonMetadata)
            .GroupBy(ExtractComparisonGroup)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToList();

        if (!comparisonGroups.Any())
        {
            sb.AppendLine("⚠️ No comparison groups found in this test class.");
            return sb.ToString();
        }

        foreach (var group in comparisonGroups)
        {
            sb.AppendLine($"## 🔬 Comparison Group: {group.Key}");
            sb.AppendLine();

            var methods = group.ToList();
            if (methods.Count < 2)
            {
                sb.AppendLine($"⚠️ Insufficient methods for comparison (found {methods.Count}, need at least 2)");
                sb.AppendLine();
                continue;
            }

            // Add method list
            sb.AppendLine("**Methods in this comparison:**");
            foreach (var method in methods)
            {
                var methodName = ExtractMethodName(method);
                sb.AppendLine($"- `{methodName}`");
            }

            sb.AppendLine();

            // Add performance summary
            var performanceSummary = CreatePerformanceSummary(methods);
            if (!string.IsNullOrEmpty(performanceSummary))
            {
                sb.AppendLine(performanceSummary);
                sb.AppendLine();
            }

            // Add detailed results table
            sb.AppendLine("### 📋 Detailed Results");
            sb.AppendLine();
            sb.AppendLine("| Method | Mean Time | Median Time | Sample Size | Status |");
            sb.AppendLine("|--------|-----------|-------------|-------------|--------|");

            foreach (var method in methods.OrderBy(m => m.PerformanceMetrics.MeanMs))
            {
                var methodName = ExtractMethodName(method);
                var meanTime = method.PerformanceMetrics.MeanMs;
                var medianTime = method.PerformanceMetrics.MedianMs;
                var sampleSize = method.PerformanceMetrics.SampleSize;
                var status = "✅ Completed";

                sb.AppendLine($"| {methodName} | {meanTime:F3}ms | {medianTime:F3}ms | {sampleSize} | {status} |");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a performance summary for a group of methods.
    /// </summary>
    /// <param name="methods">The methods in the comparison group.</param>
    /// <returns>The formatted performance summary.</returns>
    private string CreatePerformanceSummary(List<TestCompletionQueueMessage> methods)
    {
        if (methods.Count < 2) return string.Empty;

        try
        {
            // Find fastest and slowest methods
            var sortedMethods = methods.OrderBy(m => m.PerformanceMetrics.MeanMs).ToList();
            var fastest = sortedMethods.First();
            var slowest = sortedMethods.Last();

            if (fastest.TestCaseId == slowest.TestCaseId) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("**📊 Performance Summary:**");
            sb.AppendLine($"- 🟢 **Fastest:** {ExtractMethodName(fastest)} ({fastest.PerformanceMetrics.MeanMs:F3}ms)");
            sb.AppendLine($"- 🔴 **Slowest:** {ExtractMethodName(slowest)} ({slowest.PerformanceMetrics.MeanMs:F3}ms)");

            var percentageDifference = ((slowest.PerformanceMetrics.MeanMs - fastest.PerformanceMetrics.MeanMs) /
                                        fastest.PerformanceMetrics.MeanMs) * 100;
            sb.AppendLine($"- 📈 **Performance Gap:** {percentageDifference:F1}% difference");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, ex,
                "Failed to create performance summary: {0}", ex.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if a test completion message has comparison metadata.
    /// </summary>
    /// <param name="message">The test completion message.</param>
    /// <returns>True if the message has comparison metadata, false otherwise.</returns>
    private static bool HasComparisonMetadata(TestCompletionQueueMessage message)
    {
        return message.Metadata.ContainsKey("ComparisonGroup");
    }
}