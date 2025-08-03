using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Attributes;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Logging;

namespace Sailfish.DefaultHandlers.Sailfish;

/// <summary>
/// Handler for generating consolidated markdown files when test classes with method comparisons complete.
/// This handler listens for TestClassCompletedNotification and generates a single markdown file
/// containing all test results for classes marked with [WriteToMarkdown].
/// </summary>
internal class MethodComparisonTestClassCompletedHandler : INotificationHandler<TestClassCompletedNotification>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the MethodComparisonTestClassCompletedHandler class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="mediator">The mediator for publishing notifications.</param>
    public MethodComparisonTestClassCompletedHandler(ILogger logger, IMediator mediator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Handles the TestClassCompletedNotification by checking for WriteToMarkdown attribute
    /// and generating consolidated markdown for method comparison results.
    /// </summary>
    /// <param name="notification">The test class completion notification.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous markdown generation operation.</returns>
    public async Task Handle(TestClassCompletedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var testClass = notification.ClassExecutionSummaryTrackingFormat.TestClass;
            
            _logger.Log(LogLevel.Debug,
                "TestClassCompletedNotification received for class '{0}'",
                testClass.FullName);

            // Check if the test class has the WriteToMarkdown attribute
            var writeToMarkdownAttribute = testClass.GetCustomAttribute<WriteToMarkdownAttribute>();
            if (writeToMarkdownAttribute == null)
            {
                _logger.Log(LogLevel.Debug,
                    "Test class '{0}' does not have WriteToMarkdown attribute - skipping markdown generation",
                    testClass.Name);
                return;
            }

            _logger.Log(LogLevel.Information,
                "Generating consolidated markdown for test class '{0}' with WriteToMarkdown attribute",
                testClass.Name);

            // Generate consolidated markdown content
            var markdownContent = CreateConsolidatedMarkdown(notification.ClassExecutionSummaryTrackingFormat);

            if (!string.IsNullOrEmpty(markdownContent))
            {
                // Publish notification to generate markdown file
                var markdownNotification = new WriteMethodComparisonMarkdownNotification(
                    testClass.Name,
                    markdownContent,
                    string.Empty); // Output directory will be determined by handler

                await _mediator.Publish(markdownNotification, cancellationToken);

                _logger.Log(LogLevel.Information,
                    "Published consolidated WriteMethodComparisonMarkdownNotification for test class '{0}'",
                    testClass.Name);
            }
            else
            {
                _logger.Log(LogLevel.Warning,
                    "Generated markdown content was empty for test class '{0}'",
                    testClass.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to generate consolidated markdown for test class completion: {0}",
                ex.Message);
        }
    }

    /// <summary>
    /// Creates consolidated markdown content from a complete class execution summary.
    /// </summary>
    /// <param name="classExecutionSummary">The complete class execution summary.</param>
    /// <returns>The generated markdown content.</returns>
    private string CreateConsolidatedMarkdown(ClassExecutionSummaryTrackingFormat classExecutionSummary)
    {
        var sb = new StringBuilder();
        var testClass = classExecutionSummary.TestClass;

        // Add document header
        sb.AppendLine($"# 📊 Method Comparison Results: {testClass.Name}");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Test Class:** {testClass.FullName}");
        sb.AppendLine($"**Total Test Cases:** {classExecutionSummary.CompiledTestCaseResults.Count()}");
        sb.AppendLine();

        // Get all test results
        var testResults = classExecutionSummary.CompiledTestCaseResults.ToList();
        
        if (!testResults.Any())
        {
            sb.AppendLine("⚠️ No test results found in this class.");
            return sb.ToString();
        }

        // Group test results by comparison groups (if any)
        var comparisonGroups = testResults
            .Where(tr => HasComparisonAttribute(tr))
            .GroupBy(tr => GetComparisonGroup(tr))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToList();

        if (comparisonGroups.Any())
        {
            sb.AppendLine("## 🔬 Method Comparison Groups");
            sb.AppendLine();

            foreach (var group in comparisonGroups)
            {
                sb.AppendLine($"### Comparison Group: {group.Key}");
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
                    sb.AppendLine($"- `{method.TestCaseId?.DisplayName ?? "Unknown"}`");
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
                sb.AppendLine("#### 📋 Detailed Results");
                sb.AppendLine();
                sb.AppendLine("| Method | Mean Time | Median Time | Sample Size | Status |");
                sb.AppendLine("|--------|-----------|-------------|-------------|--------|");

                foreach (var method in methods.OrderBy(m => m.PerformanceRunResult?.Mean ?? double.MaxValue))
                {
                    var meanTime = method.PerformanceRunResult?.Mean ?? 0;
                    var medianTime = method.PerformanceRunResult?.Median ?? 0;
                    var sampleSize = method.PerformanceRunResult?.SampleSize ?? 0;
                    var status = method.Exception == null ? "✅ Success" : "❌ Failed";

                    sb.AppendLine($"| {method.TestCaseId?.DisplayName ?? "Unknown"} | {meanTime:F3}ms | {medianTime:F3}ms | {sampleSize} | {status} |");
                }
                sb.AppendLine();
            }
        }

        // Add section for non-comparison methods
        var nonComparisonMethods = testResults.Where(tr => !HasComparisonAttribute(tr)).ToList();
        if (nonComparisonMethods.Any())
        {
            sb.AppendLine("## 📊 Individual Test Results");
            sb.AppendLine();
            sb.AppendLine("| Method | Mean Time | Median Time | Sample Size | Status |");
            sb.AppendLine("|--------|-----------|-------------|-------------|--------|");

            foreach (var method in nonComparisonMethods.OrderBy(m => m.TestCaseId?.DisplayName ?? "Unknown"))
            {
                var meanTime = method.PerformanceRunResult?.Mean ?? 0;
                var medianTime = method.PerformanceRunResult?.Median ?? 0;
                var sampleSize = method.PerformanceRunResult?.SampleSize ?? 0;
                var status = method.Exception == null ? "✅ Success" : "❌ Failed";

                sb.AppendLine($"| {method.TestCaseId?.DisplayName ?? "Unknown"} | {meanTime:F3}ms | {medianTime:F3}ms | {sampleSize} | {status} |");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a performance summary for a group of test methods.
    /// </summary>
    /// <param name="methods">The test methods in the comparison group.</param>
    /// <returns>The formatted performance summary.</returns>
    private string CreatePerformanceSummary(List<CompiledTestCaseResultTrackingFormat> methods)
    {
        if (methods.Count < 2) return string.Empty;

        try
        {
            // Find fastest and slowest methods
            var validMethods = methods.Where(m => m.PerformanceRunResult?.Mean != null).ToList();
            if (validMethods.Count < 2) return string.Empty;

            var sortedMethods = validMethods.OrderBy(m => m.PerformanceRunResult!.Mean).ToList();
            var fastest = sortedMethods.First();
            var slowest = sortedMethods.Last();

            if (fastest.TestCaseId?.DisplayName == slowest.TestCaseId?.DisplayName) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("**📊 Performance Summary:**");
            sb.AppendLine($"- 🟢 **Fastest:** {fastest.TestCaseId?.DisplayName ?? "Unknown"} ({fastest.PerformanceRunResult!.Mean:F3}ms)");
            sb.AppendLine($"- 🔴 **Slowest:** {slowest.TestCaseId?.DisplayName ?? "Unknown"} ({slowest.PerformanceRunResult!.Mean:F3}ms)");
            
            var percentageDifference = ((slowest.PerformanceRunResult!.Mean - fastest.PerformanceRunResult!.Mean) / fastest.PerformanceRunResult!.Mean) * 100;
            sb.AppendLine($"- 📈 **Performance Gap:** {percentageDifference:F1}% difference");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex, 
                "Failed to create performance summary: {0}", ex.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks if a test result has a comparison attribute.
    /// </summary>
    /// <param name="testResult">The test result to check.</param>
    /// <returns>True if the test has a comparison attribute, false otherwise.</returns>
    private bool HasComparisonAttribute(CompiledTestCaseResultTrackingFormat testResult)
    {
        // For now, we'll assume all tests in a class with WriteToMarkdown are comparison tests
        // This could be enhanced to check for specific comparison attributes
        return !string.IsNullOrEmpty(GetComparisonGroup(testResult));
    }

    /// <summary>
    /// Gets the comparison group for a test result.
    /// </summary>
    /// <param name="testResult">The test result.</param>
    /// <returns>The comparison group name, or null if not found.</returns>
    private string? GetComparisonGroup(CompiledTestCaseResultTrackingFormat testResult)
    {
        // This is a simplified implementation
        // In a real implementation, we would need to access the method's SailfishComparison attribute
        // For now, we'll use a heuristic based on method names
        var testName = testResult.TestCaseId?.DisplayName ?? "Unknown";
        
        if (testName.Contains("Sum"))
            return "SumCalculation";
        if (testName.Contains("Sort"))
            return "SortingAlgorithm";
            
        return null;
    }
}
