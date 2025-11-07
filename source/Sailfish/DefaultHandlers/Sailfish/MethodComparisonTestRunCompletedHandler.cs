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
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Diagnostics.Environment;
using Sailfish.Logging;

namespace Sailfish.DefaultHandlers.Sailfish;

/// <summary>
/// Handler for generating consolidated markdown files when test runs with method comparisons complete.
/// This handler listens for TestRunCompletedNotification and generates a single markdown file
/// containing all test results from classes marked with [WriteToMarkdown] in the entire test session.
/// </summary>
internal class MethodComparisonTestRunCompletedHandler : INotificationHandler<TestRunCompletedNotification>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IEnvironmentHealthReportProvider? _healthProvider;


    /// <summary>
    /// Initializes a new instance of the MethodComparisonTestRunCompletedHandler class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="mediator">The mediator for publishing notifications.</param>
    public MethodComparisonTestRunCompletedHandler(ILogger logger, IMediator mediator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _healthProvider = null; // backward compatible path
    }

    /// <summary>
    /// Preferred constructor that can optionally receive environment health report provider via DI.
    /// </summary>
    public MethodComparisonTestRunCompletedHandler(ILogger logger, IMediator mediator, IEnvironmentHealthReportProvider healthProvider)
        : this(logger, mediator)
    {
        _healthProvider = healthProvider;
    }

    /// <summary>
    /// Handles the TestRunCompletedNotification by checking for WriteToMarkdown attributes
    /// and generating a single consolidated markdown file for the entire test session.
    /// </summary>
    /// <param name="notification">The test run completion notification.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous markdown generation operation.</returns>
    public async Task Handle(TestRunCompletedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Log(LogLevel.Debug,
                "TestRunCompletedNotification received for {0} classes",
                notification.ClassExecutionSummaries.Count());

            // Find all classes with WriteToMarkdown attribute
            var classesWithMarkdown = notification.ClassExecutionSummaries
                .Where(summary => summary.TestClass.GetCustomAttribute<WriteToMarkdownAttribute>() != null)
                .ToList();

            if (!classesWithMarkdown.Any())
            {
                _logger.Log(LogLevel.Debug,
                    "No test classes found with WriteToMarkdown attribute - skipping session markdown generation");
                return;
            }

            _logger.Log(LogLevel.Information,
                "Generating consolidated session markdown for {0} test classes with WriteToMarkdown attribute",
                classesWithMarkdown.Count);

            // Generate consolidated markdown content for the entire session
            var markdownContent = CreateSessionConsolidatedMarkdown(classesWithMarkdown);

            if (!string.IsNullOrEmpty(markdownContent))
            {
                // Generate unique session ID and timestamp
                var sessionId = Guid.NewGuid().ToString("N")[..8];
                var timestamp = DateTime.UtcNow;

                // Publish notification to generate markdown file with session-based naming
                var markdownNotification = new WriteMethodComparisonMarkdownNotification(
                    $"TestSession_{sessionId}",
                    markdownContent,
                    string.Empty) // Output directory will be determined by handler
                {
                    Timestamp = timestamp
                };

                await _mediator.Publish(markdownNotification, cancellationToken);

                _logger.Log(LogLevel.Information,
                    "Published consolidated session WriteMethodComparisonMarkdownNotification for {0} test classes",
                    classesWithMarkdown.Count);
            }
            else
            {
                _logger.Log(LogLevel.Warning,
                    "Generated session markdown content was empty for {0} test classes",
                    classesWithMarkdown.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to generate consolidated session markdown for test run completion: {0}",
                ex.Message);
        }
    }

    /// <summary>
    /// Creates consolidated markdown content from multiple class execution summaries.
    /// </summary>
    /// <param name="classExecutionSummaries">The class execution summaries from the test session.</param>
    /// <returns>The generated markdown content.</returns>
    private string CreateSessionConsolidatedMarkdown(List<ClassExecutionSummaryTrackingFormat> classExecutionSummaries)
    {
        var sb = new StringBuilder();
        var timestamp = DateTime.UtcNow;
        var sessionId = Guid.NewGuid().ToString("N")[..8];

        // Add document header
        sb.AppendLine("# 📊 Test Session Results");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Session ID:** {sessionId}");
        sb.AppendLine($"**Total Test Classes:** {classExecutionSummaries.Count}");

        // Count total test cases across all classes
        var totalTestCases = classExecutionSummaries.Sum(summary => summary.CompiledTestCaseResults.Count());
        sb.AppendLine($"**Total Test Cases:** {totalTestCases}");
        sb.AppendLine();

        // Optional environment health section (if available)
        AppendEnvironmentHealthSection(sb);
        sb.AppendLine();

        // Collect all test results from all classes
        void AppendEnvironmentHealthSection(StringBuilder sb)
        {
            try
            {
                var provider = _healthProvider;
                var report = provider?.Current;
                if (report is null) return;

                sb.AppendLine("## 🏥 Environment Health Check");
                sb.AppendLine();
                sb.AppendLine($"Score: {report.Score}/100 ({report.SummaryLabel})");

                // Show top few entries
                foreach (var e in report.Entries.Take(6))
                {
                    var icon = e.Status switch
                    {
                        HealthStatus.Pass => "✅",
                        HealthStatus.Warn => "⚠️",
                        HealthStatus.Fail => "❌",
                        _ => "❓"
                    };
                    var rec = string.IsNullOrWhiteSpace(e.Recommendation) ? string.Empty : $" — {e.Recommendation}";
                    sb.AppendLine($"- {icon} {e.Name}: {e.Status} ({e.Details}){rec}");
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Debug, ex, "Failed to append environment health section: {0}", ex.Message);
            }
        }

        var allTestResults = classExecutionSummaries
            .SelectMany(summary => summary.CompiledTestCaseResults.Select(result => new
            {
                TestResult = result,
                TestClass = summary.TestClass
            }))
            .ToList();

        if (!allTestResults.Any())
        {
            sb.AppendLine("⚠️ No test results found in this session.");
            return sb.ToString();
        }

        // Group test results by comparison groups across all classes
        var comparisonGroups = allTestResults
            .Where(tr => HasComparisonAttribute(tr.TestResult, tr.TestClass))
            .GroupBy(tr => GetComparisonGroup(tr.TestResult, tr.TestClass))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToList();

        if (comparisonGroups.Any())
        {
            foreach (var group in comparisonGroups)
            {
                sb.AppendLine($"## 🔬 Comparison Group: {group.Key}");
                sb.AppendLine();

                var methods = group.Select(g => g.TestResult).ToList();
                if (methods.Count < 2)
                {
                    sb.AppendLine($"⚠️ Insufficient methods for comparison (found {methods.Count}, need at least 2)");
                    sb.AppendLine();
                    continue;
                }

                // Generate NxN comparison matrix
                var comparisonMatrix = CreateNxNComparisonMatrix(methods);
                if (!string.IsNullOrEmpty(comparisonMatrix))
                {
                    sb.AppendLine("### Performance Comparison Matrix");
                    sb.AppendLine();
                    sb.AppendLine(comparisonMatrix);
                    sb.AppendLine();
                }

                // Add detailed results table
                sb.AppendLine("### Detailed Results");
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
        var nonComparisonMethods = allTestResults
            .Where(tr => !HasComparisonAttribute(tr.TestResult, tr.TestClass))
            .Select(tr => tr.TestResult)
            .ToList();

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
    /// Creates an NxN comparison matrix showing relative performance between all methods in a group.
    /// </summary>
    /// <param name="methods">The test methods in the comparison group.</param>
    /// <returns>The formatted NxN comparison matrix as markdown.</returns>
    private string CreateNxNComparisonMatrix(List<CompiledTestCaseResultTrackingFormat> methods)
    {
        if (methods.Count < 2) return string.Empty;

        try
        {
            // Filter to only methods with valid performance results
            var validMethods = methods.Where(m => m.PerformanceRunResult?.Mean != null).ToList();
            if (validMethods.Count < 2) return string.Empty;

            var sb = new StringBuilder();

            // Create header row
            sb.Append("|");
            sb.Append(" ".PadRight(20)); // Empty cell for row headers
            sb.Append("|");
            foreach (var method in validMethods)
            {
                var methodName = GetMethodName(method.TestCaseId?.DisplayName ?? "Unknown");
                sb.Append($" {methodName} |");
            }
            sb.AppendLine();

            // Create separator row
            sb.Append("|");
            sb.Append("-".PadRight(20, '-'));
            sb.Append("|");
            foreach (var method in validMethods)
            {
                var methodName = GetMethodName(method.TestCaseId?.DisplayName ?? "Unknown");
                sb.Append("-".PadRight(methodName.Length + 2, '-'));
                sb.Append("|");
            }
            sb.AppendLine();

            // Create data rows
            foreach (var rowMethod in validMethods)
            {
                var rowMethodName = GetMethodName(rowMethod.TestCaseId?.DisplayName ?? "Unknown");
                sb.Append($"| **{rowMethodName}**".PadRight(22));
                sb.Append("|");

                foreach (var colMethod in validMethods)
                {
                    var colMethodName = GetMethodName(colMethod.TestCaseId?.DisplayName ?? "Unknown");
                    if (rowMethod.TestCaseId?.DisplayName == colMethod.TestCaseId?.DisplayName)
                    {
                        sb.Append(" -".PadRight(colMethodName.Length + 2));
                    }
                    else
                    {
                        var comparison = CalculatePerformanceComparison(rowMethod, colMethod);
                        sb.Append($" {comparison}".PadRight(colMethodName.Length + 2));
                    }
                    sb.Append("|");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to create NxN comparison matrix: {0}", ex.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// Calculates the performance comparison between two methods.
    /// </summary>
    /// <param name="method1">The first method (row method).</param>
    /// <param name="method2">The second method (column method).</param>
    /// <returns>A string describing the relative performance (e.g., "2.3x faster", "1.8x slower").</returns>
    private string CalculatePerformanceComparison(CompiledTestCaseResultTrackingFormat method1, CompiledTestCaseResultTrackingFormat method2)
    {
        try
        {
            var mean1 = method1.PerformanceRunResult?.Mean ?? 0;
            var mean2 = method2.PerformanceRunResult?.Mean ?? 0;

            if (mean1 <= 0 || mean2 <= 0) return "N/A";

            var ratio = mean2 / mean1;

            if (ratio > 1.0)
            {
                return $"{ratio:F1}x slower";
            }
            else if (ratio < 1.0)
            {
                var inverseRatio = mean1 / mean2;
                return $"{inverseRatio:F1}x faster";
            }
            else
            {
                return "~same";
            }
        }
        catch
        {
            return "N/A";
        }
    }

    /// <summary>
    /// Extracts the method name from a test case display name.
    /// </summary>
    /// <param name="displayName">The full test case display name.</param>
    /// <returns>The method name portion.</returns>
    private string GetMethodName(string displayName)
    {
        // Handle display names like "ReadmeExample.TestMethod(N: 1)"
        var methodName = displayName;

        // Remove class name prefix if present
        var dotIndex = methodName.LastIndexOf('.');
        if (dotIndex > 0)
        {
            methodName = methodName.Substring(dotIndex + 1);
        }

        // Remove any variable parameters from the display name
        var parenIndex = methodName.IndexOf('(');
        if (parenIndex > 0)
        {
            methodName = methodName.Substring(0, parenIndex);
        }

        return methodName;
    }

    /// <summary>
    /// Checks if a test result has a comparison attribute.
    /// </summary>
    /// <param name="testResult">The test result to check.</param>
    /// <param name="testClass">The test class type.</param>
    /// <returns>True if the test has a comparison attribute, false otherwise.</returns>
    private bool HasComparisonAttribute(CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        return !string.IsNullOrEmpty(GetComparisonGroup(testResult, testClass));
    }

    /// <summary>
    /// Gets the comparison group for a test result by reading the SailfishComparison attribute.
    /// </summary>
    /// <param name="testResult">The test result.</param>
    /// <param name="testClass">The test class type.</param>
    /// <returns>The comparison group name, or null if not found.</returns>
    private string? GetComparisonGroup(CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        try
        {
            var displayName = testResult.TestCaseId?.DisplayName ?? "Unknown";
            _logger.Log(LogLevel.Debug, "Processing test case: {0}", displayName);

            var methodName = GetMethodName(displayName);
            _logger.Log(LogLevel.Debug, "Extracted method name: {0}", methodName);

            // Use reflection to find the method and read its SailfishComparison attribute
            var method = testClass.GetMethod(methodName);
            if (method == null)
            {
                _logger.Log(LogLevel.Debug, "Method '{0}' not found directly, searching all methods", methodName);

                // Try to find method by searching all methods (in case of parameter variations)
                var allMethods = testClass.GetMethods();
                method = allMethods.FirstOrDefault(m =>
                    displayName.StartsWith(m.Name) == true);

                if (method != null)
                {
                    _logger.Log(LogLevel.Debug, "Found method via search: {0}", method.Name);
                }
                else
                {
                    _logger.Log(LogLevel.Debug, "No matching method found for display name: {0}", displayName);
                }
            }

            if (method != null)
            {
                var comparisonAttribute = method.GetCustomAttribute<SailfishComparisonAttribute>();
                if (comparisonAttribute != null && !comparisonAttribute.Disabled)
                {
                    _logger.Log(LogLevel.Debug, "Found comparison group '{0}' for method '{1}'",
                        comparisonAttribute.ComparisonGroup, method.Name);
                    return comparisonAttribute.ComparisonGroup;
                }
                else
                {
                    _logger.Log(LogLevel.Debug, "No SailfishComparison attribute found for method '{0}'", method.Name);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to get comparison group for test '{0}': {1}",
                testResult.TestCaseId?.DisplayName ?? "Unknown", ex.Message);
            return null;
        }
    }
}
