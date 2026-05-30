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
using Sailfish.Diagnostics.Environment;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Results;
using MathNet.Numerics.Distributions;
using Sailfish.Analysis.SailDiff.Statistics;

using Sailfish.Execution;

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
    private readonly IRunSettings? _runSettings;
    private readonly IReproducibilityManifestProvider? _manifestProvider;
    private readonly ITimerCalibrationResultProvider? _timerProvider;



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
        _runSettings = null;
        _manifestProvider = null;
        _timerProvider = null;
    }

    /// <summary>
    /// Preferred constructor that can optionally receive environment health report provider via DI.
    /// </summary>
    public MethodComparisonTestRunCompletedHandler(ILogger logger, IMediator mediator, IEnvironmentHealthReportProvider healthProvider)
        : this(logger, mediator)
    {
        _healthProvider = healthProvider;
    }

    public MethodComparisonTestRunCompletedHandler(
        ILogger logger,
        IMediator mediator,
        IEnvironmentHealthReportProvider healthProvider,
        IRunSettings runSettings,
        IReproducibilityManifestProvider manifestProvider,
        ITimerCalibrationResultProvider timerProvider)
        : this(logger, mediator, healthProvider)
    {
        _runSettings = runSettings;
        _manifestProvider = manifestProvider;
        _timerProvider = timerProvider;
    }

    /// <summary>
    /// Full-feature constructor including run settings and reproducibility manifest provider.
    /// Autofac will select this when all dependencies are available.
    /// </summary>
    public MethodComparisonTestRunCompletedHandler(
        ILogger logger,
        IMediator mediator,
        IEnvironmentHealthReportProvider healthProvider,
        IRunSettings runSettings,
        IReproducibilityManifestProvider manifestProvider)
        : this(logger, mediator, healthProvider)
    {
        _runSettings = runSettings;
        _manifestProvider = manifestProvider;
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

            // Build and persist reproducibility manifest next to results (best-effort)
            // Do this BEFORE generating markdown so the summary can include manifest details (e.g., randomization seed)
            try
            {
                if (_runSettings != null && _manifestProvider != null)
                {
                    var baseManifest = _manifestProvider.Current ?? ReproducibilityManifest.CreateBase(_runSettings, _healthProvider?.Current);

                    // Attach timer calibration snapshot if available
                    try
                    {
                        var calib = _timerProvider?.Current;
                        if (calib != null)
                        {
                            baseManifest.TimerCalibration = ReproducibilityManifest.TimerCalibrationSnapshot.From(calib);
                        }
                    }
                    catch { /* best-effort */ }

                    baseManifest.AddMethodSnapshots(classesWithMarkdown);
                    _manifestProvider.Current = baseManifest;

                    var outputDirectory = _runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;
                    ReproducibilityManifest.WriteJson(baseManifest, outputDirectory);
                    _logger.Log(LogLevel.Information, "Reproducibility manifest written to {0}", outputDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Debug, ex, "Failed to create/write reproducibility manifest: {0}", ex.Message);
            }

            // Generate consolidated markdown content for the entire session (after manifest is available)
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
        AppendReproducibilitySummarySection(sb);
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

        void AppendReproducibilitySummarySection(StringBuilder sb)
        {
            try
            {
                var manifest = _manifestProvider?.Current;
                if (manifest is null) return;

                sb.AppendLine("## 🔁 Reproducibility Summary");
                sb.AppendLine();
                sb.AppendLine($"- Sailfish {manifest.SailfishVersion} on {manifest.DotNetRuntime}");
                sb.AppendLine($"- OS: {manifest.Os} ({manifest.OsArchitecture}/{manifest.ProcessArchitecture})");
                sb.AppendLine($"- GC: {manifest.GcMode}; JIT: {manifest.Jit}");
                if (!string.IsNullOrWhiteSpace(manifest.EnvironmentHealthLabel))
                {
                    sb.AppendLine($"- Env Health: {manifest.EnvironmentHealthScore}/100 ({manifest.EnvironmentHealthLabel})");
                }
                sb.AppendLine($"- Timer: {manifest.Timer}");

                // Timer calibration details if available
                if (manifest.TimerCalibration is not null)
                {
                    var t = manifest.TimerCalibration;
                    sb.AppendLine($"  - Calibration: freq={t.StopwatchFrequency} Hz, res≈{t.ResolutionNs:F0} ns, baseline={t.MedianTicks} ticks");
                    sb.AppendLine($"  - Jitter: RSD={t.RsdPercent:F1}% | Score={t.JitterScore}/100 | N={t.Samples} (warmup {t.Warmups})");
                }

                if (!string.IsNullOrWhiteSpace(manifest.CiSystem)) sb.AppendLine($"- CI: {manifest.CiSystem}");
                // Randomization seed (if used)
                if (manifest.Randomization?.Seed is int seedValue)
                {
                    sb.AppendLine($"- Randomization Seed: {seedValue}");
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Debug, ex, "Failed to append reproducibility summary: {0}", ex.Message);
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

        // Group test results by (TestClass, ComparisonGroup). Grouping by name alone would merge
        // same-named groups from different test classes — each class's lone baseline would be counted
        // together, and a valid per-class baseline configuration would collapse to N×N. The grouping
        // domain matches the analyzer: per-class, per-group.
        var comparisonGroups = allTestResults
            .Where(tr => HasComparisonAttribute(tr.TestResult, tr.TestClass))
            .GroupBy(tr => new ComparisonGroupKey(tr.TestClass, GetComparisonGroup(tr.TestResult, tr.TestClass)))
            .Where(g => !string.IsNullOrEmpty(g.Key.GroupName))
            .ToList();

        if (comparisonGroups.Any())
        {
            foreach (var group in comparisonGroups)
            {
                var groupName = group.Key.GroupName!;
                var className = group.Key.TestClass.Name;
                sb.AppendLine($"## 🔬 Comparison Group: {groupName} ({className})");
                sb.AppendLine();

                var members = group.ToList();
                var methods = members.Select(m => m.TestResult).ToList();
                if (methods.Count < 2)
                {
                    sb.AppendLine($"⚠️ Insufficient methods for comparison (found {methods.Count}, need at least 2)");
                    sb.AppendLine();
                    continue;
                }

                // Determine baselines (zero, one, or — incorrectly — many).
                var baselines = members
                    .Where(m => GetComparisonInfoForResult(m.TestResult, m.TestClass).IsBaseline)
                    .Select(m => m.TestResult)
                    .ToList();

                string comparisonContent;
                string sectionHeader;
                if (baselines.Count == 1)
                {
                    sectionHeader = "### Baseline Comparison";
                    comparisonContent = CreateBaselineComparisonTable(baselines[0], methods);
                }
                else
                {
                    if (baselines.Count > 1)
                    {
                        _logger.Log(LogLevel.Warning,
                            "Comparison group '{0}' in class '{1}' has {2} methods marked IsBaseline=true; expected at most one. " +
                            "Falling back to N×N comparison. The SF1301 analyzer should catch this at build time.",
                            groupName, className, baselines.Count);
                    }
                    sectionHeader = "### Performance Comparison Matrix";
                    comparisonContent = CreateNxNComparisonMatrix(methods);
                }

                if (!string.IsNullOrEmpty(comparisonContent))
                {
                    sb.AppendLine(sectionHeader);
                    sb.AppendLine();
                    sb.AppendLine(comparisonContent);
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
    /// Creates an NxN comparison matrix with ratio CIs and BH-FDR adjusted q-values.
    /// Cell value is ratio vs. row (col/row). 'Improved' = significantly faster.
    /// </summary>
    private string CreateNxNComparisonMatrix(List<CompiledTestCaseResultTrackingFormat> methods)
    {
        if (methods.Count < 2) return string.Empty;
        try
        {
            // Build stats from tracking format (use cleaned data length for N when available)
            var stats = methods
                .Where(m => m.PerformanceRunResult != null)
                .Select(m => new
                {
                    Id = m.TestCaseId?.DisplayName ?? m.PerformanceRunResult!.DisplayName,
                    Name = GetMethodName(m.TestCaseId?.DisplayName ?? m.PerformanceRunResult!.DisplayName),
                    Mean = m.PerformanceRunResult!.Mean,
                    StdDev = m.PerformanceRunResult!.StdDev,
                    N = Math.Max(1, (m.PerformanceRunResult!.DataWithOutliersRemoved?.Length ?? -1) > 0
                        ? (m.PerformanceRunResult!.DataWithOutliersRemoved?.Length ?? m.PerformanceRunResult!.SampleSize)
                        : m.PerformanceRunResult!.SampleSize)
                })
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Mean,
                    x.N,
                    SE = (x.N > 1 && x.StdDev > 0) ? x.StdDev / Math.Sqrt(x.N) : 0.0
                })
                .Where(s => s.Mean > 0)
                .ToList();

            if (stats.Count < 2) return string.Empty;

            // Compute pairwise p-values on log-ratio and apply BH-FDR
            var pMap = new Dictionary<(string A, string B), double>();
            for (var i = 0; i < stats.Count; i++)
            {
                for (var j = i + 1; j < stats.Count; j++)
                {
                    var a = stats[i];
                    var b = stats[j];
                    var p = ComputeLogRatioPValue(a.Mean, a.SE, a.N, b.Mean, b.SE, b.N);
                    if (!double.IsNaN(p) && p > 0)
                    {
                        pMap[MultipleComparisons.NormalizePair(a.Id, b.Id)] = p;
                    }
                }
            }
            var qMap = pMap.Count > 0
                ? MultipleComparisons.BenjaminiHochbergAdjust(pMap)
                : new Dictionary<(string, string), double>();

            var alpha = _runSettings?.SailDiffSettings?.Alpha ?? SailDiffSignificance.FallbackAlpha;
            var confidenceLevel = 1.0 - alpha;
            var sb = new StringBuilder();
            sb.AppendLine($"### 🔢 NxN Comparison Matrix (q-values via BH-FDR, α={alpha:0.###})");
            sb.Append("| Method |");
            foreach (var col in stats) sb.Append($" {col.Name} |");
            sb.AppendLine();
            sb.Append("|-");
            foreach (var _ in stats) sb.Append("|-|");
            sb.AppendLine();

            for (var i = 0; i < stats.Count; i++)
            {
                var row = stats[i];
                sb.Append($"| {row.Name} |");
                for (var j = 0; j < stats.Count; j++)
                {
                    if (i == j)
                    {
                        sb.Append(" — |");
                        continue;
                    }
                    var col = stats[j];
                    var (ratio, lo, hi) = MultipleComparisons.ComputeRatioCi(row.Mean, row.SE, row.N, col.Mean, col.SE, col.N, confidenceLevel);
                    qMap.TryGetValue(MultipleComparisons.NormalizePair(row.Id, col.Id), out var q);
                    var sig = SailDiffSignificance.IsSignificantPositive(q, alpha);
                    var label = sig ? (ratio < 1.0 ? "Improved" : "Slower") : "Similar";
                    var cell = $"{FormatRatio(ratio, lo, hi)}{(q > 0 ? $" q={FormatP(q)}" : "")} {label}";
                    sb.Append($" {cell} |");
                }
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine($"_Cell value is ratio vs. row (col/row). CI is {confidenceLevel:P0} on ratio. 'Improved' means significantly faster; 'Slower' significantly slower; 'Similar' not significant after FDR._");
            return sb.ToString();

            static string FormatRatio(double ratio, double? lo, double? hi)
            {
                var r = $"{ratio:0.###}x";
                if (lo.HasValue && hi.HasValue) return $"{r} [{lo.Value:0.###}–{hi.Value:0.###}]";
                return r;
            }

            static string FormatP(double p)
            {
                if (p < 1e-3) return p.ToString("0.0e-0");
                return p.ToString("0.###");
            }

            static double ComputeLogRatioPValue(double meanA, double seA, int nA, double meanB, double seB, int nB)
            {
                if (!(meanA > 0) || !(meanB > 0)) return double.NaN;
                var seLog = Math.Sqrt(Square(SafeDiv(seA, meanA)) + Square(SafeDiv(seB, meanB)));
                if (seLog <= 0) return double.NaN;
                var t = Math.Abs(Math.Log(meanB / meanA)) / seLog;
                var dof = Math.Max(1, Math.Min(Math.Max(0, nA - 1), Math.Max(0, nB - 1)));
                var cdf = StudentT.CDF(0, 1, dof, t);
                var p = 2 * Math.Max(0.0, 1.0 - cdf);
                return p;
            }

            static double SafeDiv(double a, double b) => Math.Abs(b) < double.Epsilon ? 0 : a / b;
            static double Square(double x) => x * x;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex, "Failed to create NxN comparison matrix (FDR/CI): {0}", ex.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// Creates a baseline-vs-contender comparison table (N−1 comparisons). Each contender's
    /// ratio is computed relative to the baseline method; q-values are BH-FDR adjusted across
    /// the N−1 baseline-vs-contender pairs in this group.
    /// </summary>
    private string CreateBaselineComparisonTable(
        CompiledTestCaseResultTrackingFormat baseline,
        List<CompiledTestCaseResultTrackingFormat> allMethods)
    {
        if (allMethods.Count < 2) return string.Empty;
        try
        {
            var stats = allMethods
                .Where(m => m.PerformanceRunResult != null)
                .Select(m => new
                {
                    Result = m,
                    Id = m.TestCaseId?.DisplayName ?? m.PerformanceRunResult!.DisplayName,
                    Name = GetMethodName(m.TestCaseId?.DisplayName ?? m.PerformanceRunResult!.DisplayName),
                    Mean = m.PerformanceRunResult!.Mean,
                    StdDev = m.PerformanceRunResult!.StdDev,
                    N = Math.Max(1, (m.PerformanceRunResult!.DataWithOutliersRemoved?.Length ?? -1) > 0
                        ? (m.PerformanceRunResult!.DataWithOutliersRemoved?.Length ?? m.PerformanceRunResult!.SampleSize)
                        : m.PerformanceRunResult!.SampleSize)
                })
                .Select(x => new
                {
                    x.Result,
                    x.Id,
                    x.Name,
                    x.Mean,
                    x.N,
                    SE = (x.N > 1 && x.StdDev > 0) ? x.StdDev / Math.Sqrt(x.N) : 0.0
                })
                .Where(s => s.Mean > 0)
                .ToList();

            var baselineStat = stats.FirstOrDefault(s => ReferenceEquals(s.Result, baseline));
            if (baselineStat == null || stats.Count < 2) return string.Empty;

            var contenders = stats.Where(s => !ReferenceEquals(s.Result, baseline)).ToList();
            if (contenders.Count == 0) return string.Empty;

            // BH-FDR over the N−1 baseline-vs-contender p-values
            var pMap = new Dictionary<(string A, string B), double>();
            foreach (var c in contenders)
            {
                var p = ComputeLogRatioPValue(baselineStat.Mean, baselineStat.SE, baselineStat.N, c.Mean, c.SE, c.N);
                if (!double.IsNaN(p) && p > 0)
                {
                    pMap[MultipleComparisons.NormalizePair(baselineStat.Id, c.Id)] = p;
                }
            }
            var qMap = pMap.Count > 0
                ? MultipleComparisons.BenjaminiHochbergAdjust(pMap)
                : new Dictionary<(string, string), double>();

            var alpha = _runSettings?.SailDiffSettings?.Alpha ?? SailDiffSignificance.FallbackAlpha;
            var confidenceLevel = 1.0 - alpha;
            var sb = new StringBuilder();
            sb.AppendLine($"### 📐 Baseline-vs-Contender (baseline = `{baselineStat.Name}`, q-values via BH-FDR, α={alpha:0.###})");
            sb.AppendLine($"| Method | Mean | Ratio vs Baseline | {confidenceLevel:P0} CI | q-value | Label |");
            sb.AppendLine("|--------|------|-------------------|--------|---------|-------|");
            sb.AppendLine($"| `{baselineStat.Name}` _(baseline)_ | {baselineStat.Mean:0.###}ms | — | — | — | — |");

            foreach (var c in contenders.OrderBy(s => s.Mean))
            {
                var (ratio, lo, hi) = MultipleComparisons.ComputeRatioCi(baselineStat.Mean, baselineStat.SE, baselineStat.N, c.Mean, c.SE, c.N, confidenceLevel);
                qMap.TryGetValue(MultipleComparisons.NormalizePair(baselineStat.Id, c.Id), out var q);
                var sig = SailDiffSignificance.IsSignificantPositive(q, alpha);
                var label = sig ? (ratio < 1.0 ? "Improved" : "Slower") : "Similar";
                var ciStr = (lo.HasValue && hi.HasValue) ? $"[{lo.Value:0.###}–{hi.Value:0.###}]" : "—";
                var qStr = q > 0 ? FormatP(q) : "—";
                sb.AppendLine($"| `{c.Name}` | {c.Mean:0.###}ms | {ratio:0.###}x | {ciStr} | {qStr} | {label} |");
            }

            sb.AppendLine();
            sb.AppendLine("_Ratio is contender/baseline. 'Improved' means significantly faster than baseline; 'Slower' significantly slower; 'Similar' not significant after FDR._");
            return sb.ToString();

            static string FormatP(double p)
            {
                if (p < 1e-3) return p.ToString("0.0e-0");
                return p.ToString("0.###");
            }

            static double ComputeLogRatioPValue(double meanA, double seA, int nA, double meanB, double seB, int nB)
            {
                if (!(meanA > 0) || !(meanB > 0)) return double.NaN;
                var seLog = Math.Sqrt(Square(SafeDiv(seA, meanA)) + Square(SafeDiv(seB, meanB)));
                if (seLog <= 0) return double.NaN;
                var t = Math.Abs(Math.Log(meanB / meanA)) / seLog;
                var dof = Math.Max(1, Math.Min(Math.Max(0, nA - 1), Math.Max(0, nB - 1)));
                var cdf = StudentT.CDF(0, 1, dof, t);
                var p = 2 * Math.Max(0.0, 1.0 - cdf);
                return p;
            }

            static double SafeDiv(double a, double b) => Math.Abs(b) < double.Epsilon ? 0 : a / b;
            static double Square(double x) => x * x;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex, "Failed to create baseline comparison table: {0}", ex.Message);
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
            var method = testClass.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (method == null)
            {
                _logger.Log(LogLevel.Debug, "Method '{0}' not found directly, searching all methods", methodName);

                // Try to find method by searching all methods (in case of parameter variations or non-public types)
                var allMethods = testClass.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                method = allMethods.FirstOrDefault(m =>
                    string.Equals(m.Name, methodName, StringComparison.Ordinal) ||
                    displayName.StartsWith(m.Name, StringComparison.Ordinal));

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
                var (group, _) = ReadComparisonInfo(method);
                if (!string.IsNullOrEmpty(group))
                {
                    _logger.Log(LogLevel.Debug, "Found comparison group '{0}' for method '{1}'", group, method.Name);
                    return group;
                }
                _logger.Log(LogLevel.Debug, "No comparison attribute found for method '{0}'", method.Name);
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

    /// <summary>
    /// Resolves the comparison group + baseline flag for a method, preferring the new
    /// <see cref="SailfishMethodAttribute"/> shape and falling back to the obsolete
    /// <see cref="SailfishComparisonAttribute"/> during the deprecation window.
    /// </summary>
    private static (string? Group, bool IsBaseline) ReadComparisonInfo(MethodInfo method)
    {
        var methodAttr = method.GetCustomAttribute<SailfishMethodAttribute>();
        if (methodAttr != null && !string.IsNullOrEmpty(methodAttr.ComparisonGroup))
        {
            return (methodAttr.ComparisonGroup, methodAttr.IsBaseline);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        var legacyAttr = method.GetCustomAttribute<SailfishComparisonAttribute>();
#pragma warning restore CS0618
        if (legacyAttr != null && !legacyAttr.Disabled)
        {
            // Legacy attribute had no baseline concept — treat all members as N×N participants.
            return (legacyAttr.ComparisonGroup, false);
        }

        return (null, false);
    }

    /// <summary>
    /// Returns the comparison info (group + baseline flag) for a single test result,
    /// performing the same method lookup as <see cref="GetComparisonGroup"/>.
    /// </summary>
    private (string? Group, bool IsBaseline) GetComparisonInfoForResult(
        CompiledTestCaseResultTrackingFormat testResult, Type testClass)
    {
        try
        {
            var displayName = testResult.TestCaseId?.DisplayName ?? "Unknown";
            var methodName = GetMethodName(displayName);

            var method = testClass.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (method == null)
            {
                var allMethods = testClass.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                method = allMethods.FirstOrDefault(m =>
                    string.Equals(m.Name, methodName, StringComparison.Ordinal) ||
                    displayName.StartsWith(m.Name, StringComparison.Ordinal));
            }

            return method != null ? ReadComparisonInfo(method) : (null, false);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex,
                "Failed to resolve comparison info for test '{0}': {1}",
                testResult.TestCaseId?.DisplayName ?? "Unknown", ex.Message);
            return (null, false);
        }
    }
}
