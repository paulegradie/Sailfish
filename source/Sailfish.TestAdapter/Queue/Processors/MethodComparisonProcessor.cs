﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter;
using Sailfish.TestAdapter.Execution;
using Sailfish.TestAdapter.Handlers.FrameworkHandlers;
using Sailfish.TestAdapter.Queue.Contracts;

namespace Sailfish.TestAdapter.Queue.Processors;

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
    private readonly IAdapterSailDiff _sailDiff;
    private readonly IMediator _mediator;
    private readonly ITestCaseBatchingService _batchingService;
    private readonly MethodComparisonBatchProcessor _batchProcessor;
    private readonly ISailDiffUnifiedFormatter _unifiedFormatter;

    /// <summary>
    /// Initializes a new instance of the MethodComparisonProcessor.
    /// </summary>
    /// <param name="sailDiff">The SailDiff service for performing performance comparisons.</param>
    /// <param name="mediator">The mediator for publishing notifications.</param>
    /// <param name="batchingService">The batching service for grouping test cases.</param>
    /// <param name="batchProcessor">The batch processor for handling comparison groups.</param>
    /// <param name="unifiedFormatter">The unified formatter for consistent output formatting.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public MethodComparisonProcessor(
        IAdapterSailDiff sailDiff,
        IMediator mediator,
        ITestCaseBatchingService batchingService,
        MethodComparisonBatchProcessor batchProcessor,
        ISailDiffUnifiedFormatter unifiedFormatter,
        ILogger logger) : base(logger)
    {
        _sailDiff = sailDiff ?? throw new ArgumentNullException(nameof(sailDiff));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _batchingService = batchingService ?? throw new ArgumentNullException(nameof(batchingService));
        _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
        _unifiedFormatter = unifiedFormatter ?? throw new ArgumentNullException(nameof(unifiedFormatter));
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
    protected override async Task ProcessTestCompletionCore(TestCompletionQueueMessage message, CancellationToken cancellationToken)
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
            await EnsureComparisonBatchingStrategy(cancellationToken);

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
    }

    /// <summary>
    /// Determines if we're in a context where method comparison is possible.
    /// This helps detect individual test runs where comparison partners aren't available.
    /// </summary>
    private async Task<bool> IsComparisonContextAvailable(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Extract the test class name to check if we're running the full class
            var testClassName = message.TestCaseId.Contains('.')
                ? message.TestCaseId.Substring(0, message.TestCaseId.LastIndexOf('.'))
                : "Unknown";

            // Get the batch for this test class
            var classBatchId = $"TestClass_{testClassName}";
            var classBatch = await _batchingService.GetBatchAsync(classBatchId, cancellationToken);

            if (classBatch == null)
            {
                Logger.Log(LogLevel.Debug,
                    "No class batch found for '{0}' - likely running individual test", testClassName);
                return false;
            }

            // Check if the batch contains multiple comparison methods
            var comparisonMethods = classBatch.TestCases
                .Where(tc => !string.IsNullOrEmpty(ExtractComparisonGroup(tc)))
                .ToList();

            if (comparisonMethods.Count <= 1)
            {
                Logger.Log(LogLevel.Debug,
                    "Class batch '{0}' contains only {1} comparison method(s) - skipping comparison",
                    classBatchId, comparisonMethods.Count);
                return false;
            }

            // Check if we have multiple methods for this comparison group
            var comparisonGroup = ExtractComparisonGroup(message);
            var groupMethods = comparisonMethods
                .Where(tc => ExtractComparisonGroup(tc) == comparisonGroup)
                .ToList();

            var hasCompleteGroup = groupMethods.Count >= 2;

            Logger.Log(LogLevel.Debug,
                "Comparison group '{0}' has {1} methods - comparison available: {2}",
                comparisonGroup, groupMethods.Count, hasCompleteGroup);

            return hasCompleteGroup;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, ex,
                "Error checking comparison context for '{0}' - defaulting to no comparison",
                message.TestCaseId);
            return false;
        }
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
    private async Task EnsureComparisonBatchingStrategy(CancellationToken cancellationToken)
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
    private async Task CheckAndProcessCompleteBatch(TestCompletionQueueMessage message, CancellationToken cancellationToken)
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
            }
            else
            {
                Logger.Log(LogLevel.Debug,
                    "Comparison batch '{0}' has insufficient methods for comparison: {1} methods",
                    batchId, comparisonMethods.Count);
            }
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
            return group?.ToString();
        }
        return null;
    }



    /// <summary>
    /// Adds comparison information to individual test output when comparison partners aren't available.
    /// </summary>
    private async Task AddComparisonInfoToIndividualTest(TestCompletionQueueMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var comparisonGroup = ExtractComparisonGroup(message);

            if (string.IsNullOrEmpty(comparisonGroup))
            {
                // Not a comparison method, nothing to do
                return;
            }

            Logger.Log(LogLevel.Information,
                "Adding comparison info for individual test '{0}' in group '{1}'",
                message.TestCaseId, comparisonGroup);

            // Find comparison partner methods
            var partnerMethods = GetComparisonPartnerMethods(message, comparisonGroup);

            // Only show message if companion tests exist but weren't run
            if (partnerMethods.Any())
            {
                // Create informational message
                var infoMessage = CreateIndividualTestComparisonMessage(comparisonGroup, partnerMethods);

                // Add message to test output
                await AddMessageToTestOutput(message, infoMessage, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, ex,
                "Failed to add comparison info for individual test '{0}': {1}",
                message.TestCaseId, ex.Message);
        }
    }

    /// <summary>
    /// Finds comparison partner methods in the same comparison group.
    /// </summary>
    private List<string> GetComparisonPartnerMethods(TestCompletionQueueMessage message, string comparisonGroup)
    {
        var partnerMethods = new List<string>();

        try
        {
            // Extract test class name and current method name
            var testClassName = message.TestCaseId.Contains('.')
                ? message.TestCaseId.Substring(0, message.TestCaseId.LastIndexOf('.'))
                : "Unknown";

            var currentMethodName = ExtractMethodName(message);

            // Get the test class type using reflection
            var testClass = GetTestClassTypeByName(testClassName);
            if (testClass == null)
            {
                Logger.Log(LogLevel.Warning, "Could not find test class type for '{0}'", testClassName);
                return partnerMethods;
            }

            // Find methods with SailfishComparison attribute in the same group
            var methods = testClass.GetMethods()
                .Where(m => m.GetCustomAttribute<SailfishComparisonAttribute>() != null)
                .ToList();

            foreach (var method in methods)
            {
                var comparisonAttr = method.GetCustomAttribute<SailfishComparisonAttribute>();
                if (comparisonAttr != null &&
                    comparisonAttr.ComparisonGroup == comparisonGroup &&
                    method.Name != currentMethodName)
                {
                    partnerMethods.Add(method.Name);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, ex,
                "Error finding comparison partner methods: {0}", ex.Message);
        }

        return partnerMethods;
    }

    /// <summary>
    /// Creates an informational message for individual comparison tests.
    /// </summary>
    private string CreateIndividualTestComparisonMessage(string comparisonGroup, List<string> partnerMethods)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("🔍 COMPARISON AVAILABLE:");

        if (partnerMethods.Count == 1)
        {
            sb.AppendLine($"Compare with: {partnerMethods[0]}");
        }
        else if (partnerMethods.Count > 1)
        {
            sb.AppendLine($"Compare with: {string.Join(", ", partnerMethods)}");
        }
        else
        {
            sb.AppendLine($"Compare with other methods in group '{comparisonGroup}'");
        }

        sb.AppendLine("Run both tests to see performance analysis.");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Adds a message to test output by modifying the formatted message metadata.
    /// </summary>
    private async Task AddMessageToTestOutput(TestCompletionQueueMessage message, string infoMessage, CancellationToken cancellationToken)
    {
        try
        {
            // Get existing formatted message or create empty one
            var existingMessage = message.Metadata.TryGetValue("FormattedMessage", out var msgObj)
                ? msgObj?.ToString() ?? string.Empty
                : string.Empty;

            // Add the comparison info to the formatted message
            var enhancedMessage = existingMessage + infoMessage;
            message.Metadata["FormattedMessage"] = enhancedMessage;

            Logger.Log(LogLevel.Information,
                "Added comparison info to test output for '{0}'", message.TestCaseId);

            // Note: The enhanced message will be picked up by the normal test completion flow
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, ex,
                "Failed to add message to test output for '{0}': {1}",
                message.TestCaseId, ex.Message);
        }
    }

    /// <summary>
    /// Gets the test class type using reflection.
    /// </summary>
    private Type? GetTestClassTypeByName(string className)
    {
        try
        {
            // Try to find the type in loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Name == className || t.FullName?.EndsWith($".{className}") == true)
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
}

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
            .Where(tc => HasComparisonMetadata(tc))
            .GroupBy(tc => ExtractComparisonGroup(tc))
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
    private async Task ProcessComparisonGroup(string groupName, List<TestCompletionQueueMessage> testCases, CancellationToken cancellationToken)
    {
        if (testCases.Count < 2)
        {
            _logger.Log(LogLevel.Warning,
                "Insufficient methods in comparison group '{0}': found {1} methods",
                groupName, testCases.Count);
            return;
        }

        _logger.Log(LogLevel.Information,
            "Processing comparison group '{0}' with {1} methods",
            groupName, testCases.Count);

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

                if (!allPairs.Contains(pairKey))
                {
                    allPairs.Add(pairKey);
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

    private bool HasComparisonMetadata(TestCompletionQueueMessage message)
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

    private string? GetTestClassNameFromMessage(TestCompletionQueueMessage message)
    {
        // Try to extract class name from test case ID (format: ClassName.MethodName)
        var testCaseId = message.TestCaseId;
        if (!string.IsNullOrEmpty(testCaseId))
        {
            var lastDotIndex = testCaseId.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                return testCaseId.Substring(0, lastDotIndex);
            }
        }

        // Try to get class name from metadata
        if (message.Metadata.TryGetValue("TestClassName", out var className) && className != null)
        {
            return className.ToString();
        }

        // Try to extract from FullyQualifiedName in metadata
        if (message.Metadata.TryGetValue("FullyQualifiedName", out var fqnObj) && fqnObj != null)
        {
            var fullyQualifiedName = fqnObj.ToString();
            if (!string.IsNullOrEmpty(fullyQualifiedName))
            {
                var lastDotIndex = fullyQualifiedName.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    var classAndMethod = fullyQualifiedName.Substring(0, lastDotIndex);
                    var secondLastDotIndex = classAndMethod.LastIndexOf('.');

                    return secondLastDotIndex >= 0
                        ? classAndMethod.Substring(secondLastDotIndex + 1)
                        : classAndMethod;
                }
            }
        }

        return "Unknown";
    }

    private string? ExtractTestClassName(TestCompletionQueueMessage message)
    {
        // Try to extract class name from test case ID (format: ClassName.MethodName)
        var testCaseId = message.TestCaseId;
        if (!string.IsNullOrEmpty(testCaseId))
        {
            var lastDotIndex = testCaseId.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                return testCaseId.Substring(0, lastDotIndex);
            }
        }

        // Try to get class name from metadata
        if (message.Metadata.TryGetValue("TestClassName", out var className) && className != null)
        {
            return className.ToString();
        }

        // Try to extract from FullyQualifiedName if available in metadata
        if (message.Metadata.TryGetValue("FullyQualifiedName", out var fqnObj) && fqnObj != null)
        {
            var fullyQualifiedName = fqnObj.ToString();
            if (!string.IsNullOrEmpty(fullyQualifiedName))
            {
                var lastDotIndex = fullyQualifiedName.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    var classAndMethod = fullyQualifiedName.Substring(0, lastDotIndex);
                    var secondLastDotIndex = classAndMethod.LastIndexOf('.');

                    return secondLastDotIndex >= 0
                        ? classAndMethod.Substring(secondLastDotIndex + 1)
                        : classAndMethod;
                }
            }
        }

        return "Unknown";
    }

    /// <summary>
    /// Determines if a full test class is being executed based on the test cases in the batch.
    /// </summary>
    private bool IsFullClassExecution(TestCaseBatch batch)
    {
        // Group test cases by class name
        var classBatches = batch.TestCases
            .GroupBy(tc => ExtractTestClassName(tc))
            .ToList();

        // Check each class to see if all its SailfishMethods are present
        foreach (var classBatch in classBatches)
        {
            var className = classBatch.Key;
            if (string.IsNullOrEmpty(className)) continue;

            var classTestCases = classBatch.ToList();

            try
            {
                // Get the test class type using reflection
                var testClass = GetTestClassType(className);
                if (testClass == null) continue;

                // Get all methods with SailfishMethodAttribute
                var sailfishMethods = testClass.GetMethods()
                    .Where(m => m.GetCustomAttribute<SailfishMethodAttribute>() != null)
                    .ToList();

                // Get executed method names from test cases
                var executedMethods = classTestCases
                    .Select(tc => ExtractMethodName(tc))
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .ToList();

                // If we have test cases for all SailfishMethods, it's a full class execution
                if (sailfishMethods.Count > 0 && sailfishMethods.Count == executedMethods.Count)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex,
                    "Failed to determine full class execution for class '{0}': {1}",
                    className, ex.Message);
            }
        }

        return false;
    }



    /// <summary>
    /// Extracts the method name from a test completion message.
    /// </summary>
    private string? ExtractMethodName(TestCompletionQueueMessage message)
    {
        // Try to get from metadata first
        if (message.Metadata.TryGetValue("MethodName", out var methodNameObj) && methodNameObj != null)
        {
            return methodNameObj.ToString();
        }

        // Try to extract from FullyQualifiedName in metadata
        if (message.Metadata.TryGetValue("FullyQualifiedName", out var fqnObj) && fqnObj != null)
        {
            var fullyQualifiedName = fqnObj.ToString();
            if (!string.IsNullOrEmpty(fullyQualifiedName))
            {
                var lastDotIndex = fullyQualifiedName.LastIndexOf('.');
                return lastDotIndex >= 0 && lastDotIndex < fullyQualifiedName.Length - 1
                    ? fullyQualifiedName.Substring(lastDotIndex + 1)
                    : null;
            }
        }

        // Try to extract from TestCaseId
        var testCaseId = message.TestCaseId;
        if (!string.IsNullOrEmpty(testCaseId))
        {
            var lastDotIndex = testCaseId.LastIndexOf('.');
            return lastDotIndex >= 0 && lastDotIndex < testCaseId.Length - 1
                ? testCaseId.Substring(lastDotIndex + 1)
                : testCaseId;
        }

        return null;
    }

    /// <summary>
    /// Gets the test class type using reflection.
    /// </summary>
    private Type? GetTestClassType(string className)
    {
        try
        {
            // Try to find the type in loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Name == className || t.FullName?.EndsWith($".{className}") == true)
                    .ToList();

                if (types.Count == 1)
                {
                    return types[0];
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to get test class type for '{0}': {1}",
                className, ex.Message);
        }

        return null;
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
                new[] { commonTestCaseId },
                new[] { commonTestCaseId },
                commonTestCaseId,
                CreateModifiedClassExecutionSummary(classExecutionSummary, beforeMethod, afterMethod, commonTestCaseId),
                CreateModifiedPerformanceResult(beforeData, commonTestCaseId));

            _logger.Log(LogLevel.Debug,
                "SailDiff comparison completed for group '{0}'. Results count: {1}",
                groupName, comparisonResult?.SailDiffResults?.Count() ?? 0);

            // Format comparison results from each method's perspective
            var beforeMethodOutput = FormatComparisonResults(comparisonResult, groupName, beforeMethod.TestCaseId, afterMethod.TestCaseId, beforeMethod.TestCaseId);
            var afterMethodOutput = FormatComparisonResults(comparisonResult, groupName, beforeMethod.TestCaseId, afterMethod.TestCaseId, afterMethod.TestCaseId);

            // Enhance test output messages with perspective-specific results
            EnhanceTestOutputWithComparison(beforeMethod, afterMethod, beforeMethodOutput, afterMethodOutput, cancellationToken);

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
    /// </summary>
    private PerformanceRunResult CreatePerformanceRunResultFromMessage(TestCompletionQueueMessage message)
    {
        var metrics = message.PerformanceMetrics;
        return new PerformanceRunResult(
            message.TestCaseId,
            metrics.MeanMs,
            metrics.StandardDeviation,
            metrics.Variance,
            metrics.MedianMs,
            metrics.RawExecutionResults,
            metrics.SampleSize,
            metrics.NumWarmupIterations,
            metrics.DataWithOutliersRemoved,
            metrics.UpperOutliers,
            metrics.LowerOutliers,
            metrics.TotalNumOutliers);
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
            new[] { modifiedCompiledResult });
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
            original.TotalNumOutliers);
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
            combinedResults.Count, beforeSummary.CompiledTestCaseResults.Count(), afterSummary.CompiledTestCaseResults.Count());

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
                return summaries.FirstOrDefault() ?? throw new InvalidOperationException("No class execution summary found in collection");
            }
        }

        throw new InvalidOperationException("Class execution summary not found in message metadata");
    }

    /// <summary>
    /// Formats SailDiff comparison results for display from a specific method's perspective using the unified formatter.
    /// </summary>
    private string FormatComparisonResults(TestCaseSailDiffResult comparisonResult, string groupName, string beforeMethodName, string afterMethodName, string perspectiveMethodName)
    {
        _logger.Log(LogLevel.Debug,
            "Formatting comparison results for group '{0}' from perspective '{1}'. ComparisonResult is null: {2}, SailDiffResults count: {3}",
            groupName, ExtractMethodName(perspectiveMethodName), comparisonResult == null, comparisonResult?.SailDiffResults?.Count() ?? 0);

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
                AlphaLevel = 0.05,
                TestType = "T-Test",
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
    /// Publishes enhanced framework notifications with comparison results.
    /// </summary>
    private async Task PublishEnhancedFrameworkNotifications(
        TestCompletionQueueMessage beforeMethod,
        TestCompletionQueueMessage afterMethod,
        CancellationToken cancellationToken)
    {
        // Create and publish enhanced framework notifications
        var beforeNotification = CreateFrameworkNotification(beforeMethod);
        var afterNotification = CreateFrameworkNotification(afterMethod);

        await _mediator.Publish(beforeNotification, cancellationToken);
        await _mediator.Publish(afterNotification, cancellationToken);
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
        var testCase = message.Metadata.TryGetValue("TestCase", out var testCaseObj) && testCaseObj is TestCase originalTestCase
            ? originalTestCase
            : throw new InvalidOperationException($"Original TestCase not found in metadata for test case '{message.TestCaseId}'");

        var startTime = message.Metadata.TryGetValue("StartTime", out var startTimeObj) && startTimeObj is DateTimeOffset start
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

/// <summary>
/// A combined class execution summary that merges results from multiple individual test case summaries.
/// This is needed for SailDiff comparisons which require access to both before and after test results.
/// </summary>
internal class CombinedClassExecutionSummary : IClassExecutionSummary
{
    public CombinedClassExecutionSummary(
        Type testClass,
        IExecutionSettings executionSettings,
        IEnumerable<ICompiledTestCaseResult> compiledTestCaseResults)
    {
        TestClass = testClass;
        ExecutionSettings = executionSettings;
        CompiledTestCaseResults = compiledTestCaseResults;
    }

    public Type TestClass { get; }
    public IExecutionSettings ExecutionSettings { get; }
    public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; }

    public IEnumerable<ICompiledTestCaseResult> GetSuccessfulTestCases()
    {
        return CompiledTestCaseResults.Where(x => x.PerformanceRunResult is not null);
    }

    public IEnumerable<ICompiledTestCaseResult> GetFailedTestCases()
    {
        return CompiledTestCaseResults.Where(x => x.PerformanceRunResult is null);
    }

    public IClassExecutionSummary FilterForSuccessfulTestCases()
    {
        return new CombinedClassExecutionSummary(TestClass, ExecutionSettings, GetSuccessfulTestCases());
    }

    public IClassExecutionSummary FilterForFailureTestCases()
    {
        return new CombinedClassExecutionSummary(TestClass, ExecutionSettings, GetFailedTestCases());
    }
}

/// <summary>
/// A wrapper for ICompiledTestCaseResult that allows modifying the PerformanceRunResult.
/// Used for method comparisons where we need to use a common test case ID.
/// </summary>
internal class ModifiedCompiledTestCaseResult : ICompiledTestCaseResult
{
    private readonly ICompiledTestCaseResult _original;
    private readonly PerformanceRunResult _modifiedPerformanceResult;

    public ModifiedCompiledTestCaseResult(ICompiledTestCaseResult original, PerformanceRunResult modifiedPerformanceResult)
    {
        _original = original;
        _modifiedPerformanceResult = modifiedPerformanceResult;
    }

    public TestCaseId? TestCaseId => _original.TestCaseId;
    public string? GroupingId => _original.GroupingId;
    public PerformanceRunResult? PerformanceRunResult => _modifiedPerformanceResult;
    public Exception? Exception => _original.Exception;
}
