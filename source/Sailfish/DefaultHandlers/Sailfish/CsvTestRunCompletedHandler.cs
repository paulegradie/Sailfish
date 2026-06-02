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
using Sailfish.Logging;
using MathNet.Numerics.Distributions;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.DefaultHandlers.Sailfish;

/// <summary>
/// Handler for generating consolidated CSV files when test runs with method comparisons complete.
/// This handler listens for TestRunCompletedNotification and generates a single CSV file
/// containing all test results from classes marked with [WriteToCsv] in the entire test session.
/// </summary>
internal class CsvTestRunCompletedHandler : INotificationHandler<TestRunCompletedNotification>
{
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IRunSettings? _runSettings;

    /// <summary>
    /// Initializes a new instance of the CsvTestRunCompletedHandler class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="mediator">The mediator for publishing notifications.</param>
    public CsvTestRunCompletedHandler(ILogger logger, IMediator mediator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _runSettings = null;
    }

    /// <summary>
    /// Preferred constructor: takes the optional <see cref="IRunSettings"/> so the BH-FDR
    /// q-value threshold honours the user-configured <c>SailDiffSettings.Alpha</c>
    /// instead of defaulting to 0.05. The DI container selects this overload when all
    /// dependencies are available (multiple ctors → MediatR picks the longest match).
    /// </summary>
    public CsvTestRunCompletedHandler(ILogger logger, IMediator mediator, IRunSettings runSettings)
        : this(logger, mediator)
    {
        _runSettings = runSettings;
    }

    /// <summary>
    /// Handles the TestRunCompletedNotification by checking for WriteToCsv attributes
    /// and generating a single consolidated CSV file for the entire test session.
    /// </summary>
    /// <param name="notification">The test run completion notification.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous CSV generation operation.</returns>
    public async Task Handle(TestRunCompletedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Log(LogLevel.Debug,
                "TestRunCompletedNotification received for {0} classes",
                notification.ClassExecutionSummaries.Count());

            // Find all classes with WriteToCsv attribute
            var classesWithCsv = notification.ClassExecutionSummaries
                .Where(summary => summary.TestClass.GetCustomAttribute<WriteToCsvAttribute>() != null)
                .ToList();

            if (!classesWithCsv.Any())
            {
                _logger.Log(LogLevel.Debug,
                    "No test classes found with WriteToCsv attribute - skipping session CSV generation");
                return;
            }

            _logger.Log(LogLevel.Information,
                "Generating consolidated session CSV for {0} test classes with WriteToCsv attribute",
                classesWithCsv.Count);

            // Generate consolidated CSV content for the entire session
            var csvContent = CreateSessionConsolidatedCsv(classesWithCsv);

            if (!string.IsNullOrEmpty(csvContent))
            {
                // Generate unique session ID and timestamp
                var sessionId = Guid.NewGuid().ToString("N")[..8];
                var timestamp = DateTime.UtcNow;

                // Publish notification to generate CSV file with session-based naming
                var csvNotification = new WriteMethodComparisonCsvNotification(
                    $"TestSession_{sessionId}",
                    csvContent,
                    string.Empty) // Output directory will be determined by handler
                {
                    Timestamp = timestamp
                };

                await _mediator.Publish(csvNotification, cancellationToken);

                _logger.Log(LogLevel.Information,
                    "Published consolidated session WriteMethodComparisonCsvNotification for {0} test classes",
                    classesWithCsv.Count);
            }
            else
            {
                _logger.Log(LogLevel.Warning,
                    "Generated CSV content was empty for {0} test classes",
                    classesWithCsv.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, ex,
                "Failed to handle TestRunCompletedNotification for CSV generation: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Creates consolidated CSV content from multiple class execution summaries.
    /// </summary>
    /// <param name="classExecutionSummaries">The class execution summaries from the test session.</param>
    /// <returns>The generated CSV content.</returns>
    private string CreateSessionConsolidatedCsv(List<ClassExecutionSummaryTrackingFormat> classExecutionSummaries)
    {
        var sb = new StringBuilder();
        var timestamp = DateTime.UtcNow;
        var sessionId = Guid.NewGuid().ToString("N")[..8];

        // Add session metadata section
        sb.AppendLine("# Session Metadata");
        sb.AppendLine("SessionId,Timestamp,TotalClasses,TotalTests");
        
        // Count total test cases across all classes
        var totalTestCases = classExecutionSummaries.Sum(summary => summary.CompiledTestCaseResults.Count());
        sb.AppendLine($"{sessionId},{timestamp:yyyy-MM-ddTHH:mm:ssZ},{classExecutionSummaries.Count},{totalTestCases}");
        sb.AppendLine();

        // Collect all test results from all classes
        var allTestResults = classExecutionSummaries
            .SelectMany(summary => summary.CompiledTestCaseResults.Select(result => new
            {
                TestResult = result,
                TestClass = summary.TestClass
            }))
            .ToList();

        if (!allTestResults.Any())
        {
            sb.AppendLine("# No test results found in this session");
            return sb.ToString();
        }

        // Add individual test results section
        sb.AppendLine("# Individual Test Results");
        sb.AppendLine("TestClass,TestMethod,MeanTime,MedianTime,StdDev,SampleSize,ComparisonGroup,Status");

        foreach (var testResult in allTestResults.OrderBy(tr => tr.TestClass.Name).ThenBy(tr => tr.TestResult.TestCaseId?.DisplayName ?? "Unknown"))
        {
            var className = testResult.TestClass.Name;
            var methodName = GetMethodName(testResult.TestResult.TestCaseId?.DisplayName ?? "Unknown");
            var meanTime = testResult.TestResult.PerformanceRunResult?.Mean ?? 0;
            var medianTime = testResult.TestResult.PerformanceRunResult?.Median ?? 0;
            var stdDev = testResult.TestResult.PerformanceRunResult?.StdDev ?? 0;
            var sampleSize = testResult.TestResult.PerformanceRunResult?.SampleSize ?? 0;
            // For the per-test row, emit the explicit group when set, otherwise the class name as
            // the implicit-group label (gives the consumer a stable, non-empty key); empty when not
            // in any comparison group.
            var rawGroup = GetComparisonGroup(testResult.TestResult, testResult.TestClass);
            var comparisonGroupLabel = rawGroup switch
            {
                null => "",
                "" => className,
                _ => rawGroup
            };
            var status = testResult.TestResult.Exception == null ? "Success" : "Failed";

            sb.AppendLine($"{className},{methodName},{meanTime:F3},{medianTime:F3},{stdDev:F3},{sampleSize},{comparisonGroupLabel},{status}");
        }
        sb.AppendLine();

        // Add method comparisons section
        AddMethodComparisonsSection(sb, allTestResults.Cast<object>().ToList());

        return sb.ToString();
    }

    /// <summary>
    /// Adds the method comparisons section to the CSV content using NxN matrix with BH-FDR q-values and ratio CIs.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="allTestResults">All test results from the session.</param>
    private void AddMethodComparisonsSection(StringBuilder sb, List<object> allTestResults)
    {
        // Always include the Method Comparisons section header for visibility (even if none found)
        sb.AppendLine("# Method Comparisons");

        // Group test results by comparison groups across all classes
        var typed = allTestResults
            .Select(o =>
            {
                dynamic d = o;
                return new { TestResult = (CompiledTestCaseResultTrackingFormat)d.TestResult, TestClass = (Type)d.TestClass };
            })
            .ToList();

        try
        {
            // Group by (TestClass, ComparisonGroup). A null group name means the method isn't in any
            // comparison group (class set DisableComparison = true and method has no explicit group);
            // empty string means the implicit class-wide group; non-empty means an explicit group.
            var comparisonGroups = typed
                .Where(tr => HasComparisonGroup(tr.TestResult, tr.TestClass))
                .GroupBy(tr => new ComparisonGroupKey(tr.TestClass, GetComparisonGroup(tr.TestResult, tr.TestClass)))
                .Where(g => g.Key.GroupName != null)
                .ToList();

            if (!comparisonGroups.Any())
            {
                sb.AppendLine("# No method comparisons found");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("ComparisonGroup,Method1,Method2,Mean1,Mean2,Ratio,CI95_Lower,CI95_Upper,q_value,Label,ChangeDescription");

            foreach (var group in comparisonGroups)
            {
                var groupName = group.Key.GroupName!;
                var className = group.Key.TestClass.Name;
                // Implicit groups (empty group name) get the class name as their CSV column label.
                var columnLabel = groupName.Length == 0 ? className : groupName;
                var displayGroupName = groupName.Length == 0 ? "(implicit class-wide)" : $"'{groupName}'";
                var members = group.ToList();
                var methods = members.Select(g => g.TestResult).ToList();
                if (methods.Count < 2) continue;

                // Build stats for each method in the group, tagging baseline membership.
                var stats = members
                    .Select(m =>
                    {
                        var tr = m.TestResult;
                        var pr = tr.PerformanceRunResult;
                        var mean = pr?.Mean ?? 0.0;
                        var n = pr?.DataWithOutliersRemoved?.Length ?? pr?.SampleSize ?? 0;
                        var stdDev = pr?.StdDev ?? 0.0;
                        var se = n > 0 ? stdDev / Math.Sqrt(n) : 0.0;
                        var name = GetMethodName(tr.TestCaseId?.DisplayName ?? "Unknown");
                        var id = tr.TestCaseId?.DisplayName ?? name;
                        var isBaseline = GetComparisonInfoForResult(tr, m.TestClass).IsBaseline;
                        return new { Name = name, Id = id, Mean = mean, SE = se, N = n, IsBaseline = isBaseline };
                    })
                    .Where(s => s.Mean > 0)
                    .ToList();

                if (stats.Count < 2) continue;

                var baselineStats = stats.Where(s => s.IsBaseline).ToList();
                var baselineMode = baselineStats.Count == 1;
                if (baselineStats.Count > 1)
                {
                    _logger.Log(LogLevel.Warning,
                        "Comparison group {0} in class '{1}' has {2} methods marked IsBaseline=true; expected at most one. " +
                        "Falling back to N×N comparison rows. The SF1301 analyzer should catch this at build time.",
                        displayGroupName, className, baselineStats.Count);
                }

                // Build the (i, j) pair list — baseline-vs-contender when one baseline, otherwise full N×N.
                var pairs = new List<(int I, int J)>();
                if (baselineMode)
                {
                    var baselineIdx = stats.IndexOf(baselineStats[0]);
                    for (var k = 0; k < stats.Count; k++)
                    {
                        if (k == baselineIdx) continue;
                        pairs.Add((baselineIdx, k));
                    }
                }
                else
                {
                    for (var i = 0; i < stats.Count; i++)
                    {
                        for (var j = i + 1; j < stats.Count; j++)
                        {
                            pairs.Add((i, j));
                        }
                    }
                }

                // Compute p-values over the selected pairs and apply BH-FDR
                var pMap = new Dictionary<(string A, string B), double>();
                foreach (var (i, j) in pairs)
                {
                    var a = stats[i];
                    var b = stats[j];
                    var p = ComputeLogRatioPValue(a.Mean, a.SE, a.N, b.Mean, b.SE, b.N);
                    if (!double.IsNaN(p) && p > 0)
                    {
                        pMap[MultipleComparisons.NormalizePair(a.Id, b.Id)] = p;
                    }
                }
                var qMap = pMap.Count > 0
                    ? MultipleComparisons.BenjaminiHochbergAdjust(pMap)
                    : new Dictionary<(string A, string B), double>();

                var alpha = _runSettings?.SailDiffSettings?.Alpha ?? SailDiffSignificance.FallbackAlpha;
                var confidenceLevel = 1.0 - alpha;

                // Emit one row per selected pair
                foreach (var (i, j) in pairs)
                {
                    var row = stats[i];
                    var col = stats[j];

                    var ci = MultipleComparisons.ComputeRatioCi(row.Mean, row.SE, row.N, col.Mean, col.SE, col.N, confidenceLevel);
                    var ratio = ci.Ratio;
                    var lo = ci.Lower;
                    var hi = ci.Upper;

                    qMap.TryGetValue(MultipleComparisons.NormalizePair(row.Id, col.Id), out var q);
                    var sig = SailDiffSignificance.IsSignificantPositive(q, alpha);
                    var label = sig ? (ratio < 1.0 ? "Improved" : "Slower") : "Similar";

                    var loStr = lo.HasValue ? lo.Value.ToString("0.###") : "";
                    var hiStr = hi.HasValue ? hi.Value.ToString("0.###") : "";
                    var qStr = q > 0 ? FormatP(q) : "";

                    var changeDesc = label == "Improved" ? "Improved" : label == "Slower" ? "Regressed" : "No Change";
                    sb.AppendLine($"{columnLabel},{row.Name},{col.Name},{row.Mean:0.###},{col.Mean:0.###},{ratio:0.###},{loStr},{hiStr},{qStr},{label},{changeDesc}");
                }
            }

            sb.AppendLine();
        }
        catch
        {
            // If anything goes wrong, at least keep the section header visible
            sb.AppendLine("# No method comparisons found");
            sb.AppendLine();
            return;
        }

        static string FormatP(double p)
        {
            if (p < 1e-3) return p.ToString("0.0e-0");
            return p.ToString("0.###");
        }

        static double ComputeLogRatioPValue(double meanA, double seA, int nA, double meanB, double seB, int nB)
        {
            if (!(meanA > 0) || !(meanB > 0)) return double.NaN;
            var seLog = Math.Sqrt(SafeDiv(seA, meanA) * SafeDiv(seA, meanA) + SafeDiv(seB, meanB) * SafeDiv(seB, meanB));
            if (seLog <= 0) return double.NaN;
            var t = Math.Abs(Math.Log(meanB / meanA)) / seLog;
            var dof = Math.Max(1, Math.Min(Math.Max(0, nA - 1), Math.Max(0, nB - 1)));
            var cdf = StudentT.CDF(0, 1, dof, t);
            var p = 2 * Math.Max(0.0, 1.0 - cdf);
            return p;
        }

        static double SafeDiv(double a, double b) => Math.Abs(b) < double.Epsilon ? 0 : a / b;
    }

    /// <summary>
    /// Calculates the performance comparison between two methods.
    /// </summary>
    /// <param name="method1">The first method.</param>
    /// <param name="method2">The second method.</param>
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
    /// Determines the change description based on performance comparison.
    /// </summary>
    /// <param name="mean1">Mean time of first method.</param>
    /// <param name="mean2">Mean time of second method.</param>
    /// <returns>Change description (Improved, Regressed, or No Change).</returns>
    private string DetermineChangeDescription(double mean1, double mean2)
    {
        if (mean1 <= 0 || mean2 <= 0) return "Unknown";

        var ratio = mean2 / mean1;
        const double threshold = 0.05; // 5% threshold for significance

        if (ratio > (1.0 + threshold))
        {
            return "Regressed"; // Method2 is slower than Method1
        }
        else if (ratio < (1.0 - threshold))
        {
            return "Improved"; // Method2 is faster than Method1
        }
        else
        {
            return "No Change";
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
    /// Returns true when the test result belongs to a comparison group — either an explicit
    /// <c>ComparisonGroup</c> on the method, or the implicit class-wide group (when the enclosing
    /// <c>[Sailfish]</c> class does not set <c>DisableComparison = true</c>).
    /// </summary>
    private bool HasComparisonGroup(CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        return GetComparisonGroup(testResult, testClass) != null;
    }

    /// <summary>
    /// Returns the comparison-group label for a test result:
    ///   <list type="bullet">
    ///     <item><description><c>null</c> — the method is not in any comparison group.</description></item>
    ///     <item><description>empty string — the method is in the implicit class-wide group.</description></item>
    ///     <item><description>non-empty string — the method's explicit <c>ComparisonGroup</c>.</description></item>
    ///   </list>
    /// </summary>
    private string? GetComparisonGroup(CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        try
        {
            var method = ResolveMethod(testResult, testClass);
            return method != null ? ReadComparisonInfo(method, testClass).Group : null;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to get comparison group for test '{0}': {1}",
                testResult.TestCaseId?.DisplayName ?? "Unknown", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Resolves the comparison group (with implicit-group semantics) + baseline flag for a method.
    /// </summary>
    private static (string? Group, bool IsBaseline) ReadComparisonInfo(MethodInfo method, Type testClass)
    {
        var methodAttr = method.GetCustomAttribute<SailfishMethodAttribute>();
        if (methodAttr is null) return (null, false);

        if (!string.IsNullOrEmpty(methodAttr.ComparisonGroup))
        {
            return (methodAttr.ComparisonGroup, methodAttr.IsBaseline);
        }

        var classAttr = testClass.GetCustomAttribute<SailfishAttribute>();
        if (classAttr is not null && !classAttr.DisableComparison)
        {
            return (string.Empty, methodAttr.IsBaseline);
        }

        return (null, methodAttr.IsBaseline);
    }

    /// <summary>
    /// Returns the comparison info (group + baseline flag) for a single test result, doing the same
    /// method lookup as <see cref="GetComparisonGroup"/>.
    /// </summary>
    private (string? Group, bool IsBaseline) GetComparisonInfoForResult(
        CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        try
        {
            var method = ResolveMethod(testResult, testClass);
            return method != null ? ReadComparisonInfo(method, testClass) : (null, false);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to resolve comparison info for test '{0}': {1}",
                testResult.TestCaseId?.DisplayName ?? "Unknown", ex.Message);
            return (null, false);
        }
    }

    /// <summary>
    /// Finds the <see cref="MethodInfo"/> on <paramref name="testClass"/> for the test case.
    /// </summary>
    private MethodInfo? ResolveMethod(CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        var displayName = testResult.TestCaseId?.DisplayName ?? "Unknown";
        var methodName = GetMethodName(displayName);

        var method = testClass.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (method != null) return method;

        var allMethods = testClass.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return allMethods.FirstOrDefault(m =>
            string.Equals(m.Name, methodName, StringComparison.Ordinal) ||
            displayName.StartsWith(m.Name, StringComparison.Ordinal));
    }
}
