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

        // Group test cases by comparison group. Failed members are excluded:
        // FrameworkPublishingProcessor already publishes them as Failed (see
        // its IsComparisonMethod guard), so re-publishing here would emit a
        // duplicate notification, and a failed case has no usable samples to
        // contribute to an N×N comparison anyway.
        var comparisonGroups = batch.TestCases
            .Where(HasComparisonMetadata)
            .Where(tc => tc.TestResult.IsSuccess)
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
                    group.Key ?? "null", groupMethods.Count);
                await ProcessComparisonGroup(group.Key!, groupMethods, cancellationToken);
            }
            else
            {
                // A single-method comparison group still needs to be published as a regular
                // pass/fail framework notification — otherwise VSTest/Rider sees TestOutcome.None
                // and renders "Inconclusive" because FrameworkPublishingProcessor deferred to us.
                _logger.Log(LogLevel.Information,
                    "Publishing single-method comparison group '{0}' without comparison enhancement",
                    group.Key ?? "null");
                await PublishEnhancedFrameworkNotificationsForGroup(groupMethods, cancellationToken);
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

        // Compare only WITHIN the same variable set (same problem size). Pairing across different
        // SailfishVariable values — e.g. (N: 100) vs (N: 10000) — mixes problem sizes and is
        // meaningless, so each distinct variable set forms its own comparison cohort.
        var cohorts = testCases
            .GroupBy(tc => ExtractVariableSection(tc.TestCaseId), StringComparer.Ordinal)
            .ToList();

        foreach (var cohort in cohorts)
        {
            var members = cohort.ToList();
            if (members.Count < 2)
            {
                _logger.Log(LogLevel.Debug,
                    "Comparison cohort '{0}' in group '{1}' has fewer than 2 methods ({2}); nothing to compare",
                    cohort.Key, groupName, members.Count);
                continue;
            }

            // A method flagged [SailfishMethod(IsBaseline = true)] is carried as ComparisonRole="Baseline"
            // (set during discovery in TestCaseItemCreator and forwarded through the message mappers).
            var baselines = members.Where(IsBaselineRole).ToList();

            if (baselines.Count == 1)
            {
                // Baseline-vs-contender: every other method is reported relative to the single baseline,
                // and the baseline-flagged method is always the one named "baseline".
                var baseline = baselines[0];
                foreach (var contender in members.Where(m => !ReferenceEquals(m, baseline)))
                {
                    CompareBaselineToContender(baseline, contender, groupName);
                }
            }
            else
            {
                if (baselines.Count > 1)
                {
                    _logger.Log(LogLevel.Warning,
                        "Comparison cohort '{0}' in group '{1}' has {2} methods marked IsBaseline=true; expected at most one. " +
                        "Falling back to N×N. The SF1301 analyzer should catch this at build time.",
                        cohort.Key, groupName, baselines.Count);
                }

                // No designated baseline → full N×N, each method reported from its own perspective.
                for (var i = 0; i < members.Count; i++)
                for (var j = i + 1; j < members.Count; j++)
                {
                    CompareNxNPair(members[i], members[j], groupName);
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
    /// True when the message is the comparison group's designated baseline
    /// (a <c>[SailfishMethod(IsBaseline = true)]</c> method, carried as ComparisonRole="Baseline").
    /// </summary>
    private bool IsBaselineRole(TestCompletionQueueMessage message)
        => string.Equals(ExtractComparisonRole(message), "Baseline", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The variable-set section of a TestCaseId (e.g. <c>"(N: 100)"</c>), or <c>""</c> when the method
    /// is not parameterized. Comparisons are scoped to a single variable set so different problem sizes
    /// are never compared against each other.
    /// </summary>
    private static string ExtractVariableSection(string testCaseId)
    {
        var idx = testCaseId.IndexOf('(');
        return idx >= 0 ? testCaseId.Substring(idx) : string.Empty;
    }

    /// <summary>
    /// Compares a single contender against the group's baseline and accumulates one baseline-oriented
    /// verdict (the baseline is always named "baseline") onto BOTH the contender's and the baseline's rows.
    /// </summary>
    private void CompareBaselineToContender(
        TestCompletionQueueMessage baseline,
        TestCompletionQueueMessage contender,
        string groupName)
    {
        try
        {
            // before = baseline, after = contender ⇒ MeanBefore is the baseline's mean (no swap needed).
            var comparisonResult = ComputeDiff(baseline, contender, groupName);
            StorePairwisePValue(baseline, contender, comparisonResult);

            var output = FormatOriented(comparisonResult, groupName, primary: baseline, compared: contender, swap: false);

            // The same baseline-oriented line is correct from either row's point of view.
            EnhanceTestOutputWithComparison(baseline, contender, output, output, CancellationToken.None);

            _logger.Log(LogLevel.Information,
                "Completed baseline comparison for group '{0}': {1} vs baseline {2}",
                groupName, contender.TestCaseId, baseline.TestCaseId);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed baseline comparison for group '{0}': {1}", groupName, ex.Message);
        }
    }

    /// <summary>
    /// Compares two methods with no designated baseline (N×N). Each method's row shows the pair from
    /// its own perspective, with the statistics oriented so the named baseline always carries its own mean.
    /// </summary>
    private void CompareNxNPair(
        TestCompletionQueueMessage methodA,
        TestCompletionQueueMessage methodB,
        string groupName)
    {
        try
        {
            // before = A, after = B ⇒ MeanBefore is A's mean.
            var comparisonResult = ComputeDiff(methodA, methodB, groupName);
            StorePairwisePValue(methodA, methodB, comparisonResult);

            // A's row: A is the reference (no swap). B's row: B is the reference (swap before/after).
            var outputA = FormatOriented(comparisonResult, groupName, primary: methodA, compared: methodB, swap: false);
            var outputB = FormatOriented(comparisonResult, groupName, primary: methodB, compared: methodA, swap: true);

            EnhanceTestOutputWithComparison(methodA, methodB, outputA, outputB, CancellationToken.None);

            _logger.Log(LogLevel.Information,
                "Completed comparison for group '{0}': {1} vs {2}",
                groupName, methodA.TestCaseId, methodB.TestCaseId);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to perform comparison for group '{0}': {1}", groupName, ex.Message);
        }
    }

    /// <summary>
    /// Runs the SailDiff comparison for an ordered (before, after) pair. The result's
    /// MeanBefore/RawDataBefore describe <paramref name="beforeMethod"/>.
    /// </summary>
    private TestCaseSailDiffResult ComputeDiff(
        TestCompletionQueueMessage beforeMethod,
        TestCompletionQueueMessage afterMethod,
        string groupName)
    {
        var beforeData = CreatePerformanceRunResultFromMessage(beforeMethod);
        var classExecutionSummary = CreateCombinedClassExecutionSummary(beforeMethod, afterMethod);

        // A common test case ID lets SailDiff treat the two distinct methods as before/after versions.
        var commonTestCaseId = $"Comparison_{groupName}";

        _logger.Log(LogLevel.Debug,
            "Calling SailDiff.ComputeTestCaseDiff for group '{0}' with before: '{1}', after: '{2}', using common ID: '{3}'",
            groupName, beforeMethod.TestCaseId, afterMethod.TestCaseId, commonTestCaseId);

        return _sailDiff.ComputeTestCaseDiff(
            [commonTestCaseId],
            [commonTestCaseId],
            commonTestCaseId,
            CreateModifiedClassExecutionSummary(classExecutionSummary, beforeMethod, afterMethod, commonTestCaseId),
            CreateModifiedPerformanceResult(beforeData, commonTestCaseId));
    }

    /// <summary>
    /// Stores the pairwise p-value (and the configured alpha) on both messages for the later
    /// BH-FDR adjustment of the N×N matrix.
    /// </summary>
    private void StorePairwisePValue(TestCompletionQueueMessage a, TestCompletionQueueMessage b, TestCaseSailDiffResult? comparisonResult)
    {
        var pNullable = comparisonResult?.SailDiffResults?.FirstOrDefault()?.TestResultsWithOutlierAnalysis?.StatisticalTestResult?.PValue;
        if (!pNullable.HasValue) return;

        var fdrAlpha = comparisonResult?.TestSettings?.Alpha
                       ?? Sailfish.Analysis.SailDiff.Statistics.SailDiffSignificance.FallbackAlpha;
        UpdatePairwisePValueMetadata(a, b, pNullable.Value, fdrAlpha);
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
            original.MarginOfError,
            original.ConfidenceIntervals);
    }

    /// <summary>
    /// Creates a combined class execution summary from both before and after method messages.
    /// This ensures SailDiff has access to both test results for comparison.
    /// </summary>
    private CombinedClassExecutionSummary CreateCombinedClassExecutionSummary(
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
    /// Formats a comparison for display with <paramref name="primary"/> named the baseline and
    /// <paramref name="compared"/> the contender. The statistics are oriented (see
    /// <see cref="OrientStatistics"/>) so MeanBefore/RawDataBefore always describe
    /// <paramref name="primary"/> — the named baseline therefore always carries its own mean, which is
    /// what makes the verdict direction correct. <paramref name="swap"/> is true when
    /// <paramref name="primary"/> was the SailDiff "after" operand.
    /// </summary>
    private string FormatOriented(
        TestCaseSailDiffResult? comparisonResult,
        string groupName,
        TestCompletionQueueMessage primary,
        TestCompletionQueueMessage compared,
        bool swap)
    {
        if (comparisonResult?.SailDiffResults?.Any() != true)
        {
            return "\n❌ No comparison results available\n";
        }

        var result = comparisonResult.SailDiffResults.First();
        var statistics = OrientStatistics(result.TestResultsWithOutlierAnalysis.StatisticalTestResult, swap);
        var primaryMethod = ExtractMethodName(primary.TestCaseId);
        var comparedMethod = ExtractMethodName(compared.TestCaseId);

        var comparisonData = new SailDiffComparisonData
        {
            GroupName = groupName,
            PrimaryMethodName = primaryMethod,
            ComparedMethodName = comparedMethod,
            Statistics = statistics,
            Metadata = new ComparisonMetadata
            {
                SampleSize = statistics.SampleSizeBefore,
                AlphaLevel = comparisonResult.TestSettings.Alpha,
                TestType = GetTestTypeDisplayName(comparisonResult.TestSettings.TestType),
                OutliersRemoved = (result.TestResultsWithOutlierAnalysis.Sample1?.TotalNumOutliers ?? 0) +
                                  (result.TestResultsWithOutlierAnalysis.Sample2?.TotalNumOutliers ?? 0)
            },
            // Statistics are pre-oriented to match the named methods, so no perspective swap is needed
            // downstream (the historical perspective heuristic in the formatters is a no-op).
            IsPerspectiveBased = false
        };

        var formattedOutput = _unifiedFormatter.Format(comparisonData, OutputContext.Ide);

        _logger.Log(LogLevel.Debug,
            "Unified formatter generated output for baseline '{0}' vs contender '{1}'. Significance: {2}, Change: {3:F1}%",
            primaryMethod, comparedMethod, formattedOutput.Significance, formattedOutput.PercentageChange);

        return formattedOutput.FullOutput;
    }

    /// <summary>
    /// Returns the test result oriented so MeanBefore/MedianBefore/RawDataBefore describe the method
    /// being named the baseline. When <paramref name="swap"/> is true the before/after sides are
    /// exchanged. The p-value and "No Change" detection are orientation-symmetric and preserved as-is.
    /// </summary>
    private static StatisticalTestResult OrientStatistics(StatisticalTestResult s, bool swap)
    {
        if (!swap) return s;

        return new StatisticalTestResult(
            meanBefore: s.MeanAfter,
            meanAfter: s.MeanBefore,
            medianBefore: s.MedianAfter,
            medianAfter: s.MedianBefore,
            testStatistic: s.TestStatistic,
            pValue: s.PValue,
            changeDescription: s.ChangeDescription,
            sampleSizeBefore: s.SampleSizeAfter,
            sampleSizeAfter: s.SampleSizeBefore,
            rawDataBefore: s.RawDataAfter,
            rawDataAfter: s.RawDataBefore,
            additionalResults: s.AdditionalResults)
        {
            EffectSize = s.EffectSize,
            Difference = s.Difference,
            QValue = s.QValue,
            MinimumDetectableEffectPercent = s.MinimumDetectableEffectPercent
        };
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
        const string comparisonResultsKey = "AccumulatedComparisons";

        // Get existing accumulated comparisons
        if (method.Metadata.TryGetValue(comparisonResultsKey, out var existingObj) &&
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
            method.Metadata[comparisonResultsKey] = new List<string> { newComparisonOutput };
            _logger.Log(LogLevel.Debug,
                "Created new comparison list for '{0}' with first comparison",
                method.TestCaseId);
        }

        // Update the formatted message with all accumulated comparisons
        var allComparisons = (List<string>)method.Metadata[comparisonResultsKey];
        var combinedOutput = string.Join("", allComparisons);

        // Check if there's any original non-comparison content to preserve
        var originalContent = "";
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

    private static void UpdatePairwisePValueMetadata(TestCompletionQueueMessage a, TestCompletionQueueMessage b, double pValue, double alpha)
    {
        const string key = "PairwisePValues";
        static void Update(TestCompletionQueueMessage msg, string otherId, double p, double a)
        {
            if (!msg.Metadata.TryGetValue(key, out var obj) || obj is not Dictionary<string, double> dict)
            {
                dict = new Dictionary<string, double>(StringComparer.Ordinal);
                msg.Metadata[key] = dict;
            }
            dict[otherId] = p;
            // Single source of truth for the significance threshold downstream formatters use.
            // Without this, the N×N matrix would use a hardcoded 0.05 even if the user configured
            // a tighter or looser alpha. See SailDiffSignificance.MetadataKey.
            msg.Metadata[Sailfish.Analysis.SailDiff.Statistics.SailDiffSignificance.MetadataKey] = a;
        }
        Update(a, b.TestCaseId, pValue, alpha);
        Update(b, a.TestCaseId, pValue, alpha);
    }

}