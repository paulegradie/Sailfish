using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;

using Sailfish.Results;

namespace Sailfish.Presentation;

public interface IMarkdownTableConverter
{
    string ConvertToMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries);

    string ConvertToMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries, Func<IClassExecutionSummary, bool> summaryFilter);

    string ConvertToEnhancedMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries);

    string ConvertToEnhancedMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries, Func<IClassExecutionSummary, bool> summaryFilter);

    string ConvertScaleFishResultToMarkdown(IEnumerable<ScalefishClassModel> testClassComplexityResults);
}

public class MarkdownTableConverter : IMarkdownTableConverter
{
    private readonly ISailDiffUnifiedFormatter? _unifiedFormatter;
    private readonly IReproducibilityManifestProvider? _manifestProvider;
    private readonly IRunSettings? _runSettings;


    public MarkdownTableConverter()
    {
        // Default constructor for backward compatibility
        _unifiedFormatter = null;
        _manifestProvider = null;
        _runSettings = null;
    }

    public MarkdownTableConverter(ISailDiffUnifiedFormatter unifiedFormatter)
    {
        _unifiedFormatter = unifiedFormatter ?? throw new ArgumentNullException(nameof(unifiedFormatter));
        _manifestProvider = null;
        _runSettings = null;
    }

    // IRunSettings is optional so existing callers/tests keep working; DI injects the registered
    // instance and uses it to gate the inline distribution plots.
    public MarkdownTableConverter(ISailDiffUnifiedFormatter unifiedFormatter, IReproducibilityManifestProvider manifestProvider, IRunSettings? runSettings = null)
    {
        _unifiedFormatter = unifiedFormatter ?? throw new ArgumentNullException(nameof(unifiedFormatter));
        _manifestProvider = manifestProvider ?? throw new ArgumentNullException(nameof(manifestProvider));
        _runSettings = runSettings;
    }

    // Decimals to show within the auto-selected time unit (e.g. "1.100 µs").
    private const int Decimals = 3;

    // Picks one magnitude-appropriate time unit for a group from its means/medians/std-devs.
    private static DurationUnit SelectGroupUnit(IEnumerable<ICompiledTestCaseResult> groupResults)
    {
        return DurationFormatter.SelectUnit(
            groupResults
                .Where(r => r.PerformanceRunResult != null)
                .SelectMany(r => new[]
                {
                    r.PerformanceRunResult!.Mean,
                    r.PerformanceRunResult!.Median,
                    r.PerformanceRunResult!.StdDev
                }));
    }

    // Builds the per-group descriptive table with Mean/Median/StdDev in a shared unit (carried in
    // the header). Variance stays raw — it is ms², not a duration.
    private static string BuildStatsTable(string typeName, IReadOnlyList<ICompiledTestCaseResult> groupResults, int sampleSize, DurationUnit unit)
    {
        var unitLabel = DurationFormatter.UnitLabel(unit);
        return groupResults.ToStringTable(
            typeName,
            new List<string> { "", "", "", "", "" },
            new List<string>
            {
                "Display Name",
                $"Mean ({unitLabel})",
                $"Median ({unitLabel})",
                $"StdDev ({unitLabel}, N={sampleSize})",
                "Variance"
            },
            u => u.TestCaseId!.DisplayName,
            u => DurationFormatter.Format(u.PerformanceRunResult!.Mean, unit, Decimals),
            u => DurationFormatter.Format(u.PerformanceRunResult!.Median, unit, Decimals),
            u => DurationFormatter.Format(u.PerformanceRunResult!.StdDev, unit, Decimals),
            u => u.PerformanceRunResult!.Variance);
    }

    public string ConvertToMarkdownTableString(
        IEnumerable<IClassExecutionSummary> executionSummaries,
        Func<IClassExecutionSummary, bool> summaryFilter)
    {
        var filteredSummaries = executionSummaries.Where(summaryFilter);
        return ConvertToMarkdownTableString(filteredSummaries);
    }

    public string ConvertToEnhancedMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries)
    {
        return ConvertToEnhancedMarkdownTableString(executionSummaries, _ => true);
    }

    public string ConvertToEnhancedMarkdownTableString(
        IEnumerable<IClassExecutionSummary> executionSummaries,
        Func<IClassExecutionSummary, bool> summaryFilter)
    {
        var filteredSummaries = executionSummaries.Where(summaryFilter);
        return CreateEnhancedMarkdownOutput(filteredSummaries);
    }

    public string ConvertToMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries)
    {
        var stringBuilder = new StringBuilder();

        var allExceptions = new List<Exception>();
        foreach (var result in executionSummaries)
        {
            AppendResults(result.TestClass.Name, result.CompiledTestCaseResults, stringBuilder);
            allExceptions.AddRange(result.CompiledTestCaseResults.Where(x => x.Exception is not null).Select(x => x.Exception).Cast<Exception>().ToList());
        }

        AppendExceptions(allExceptions, stringBuilder);

        return stringBuilder.ToString();
    }

    private string CreateEnhancedMarkdownOutput(IEnumerable<IClassExecutionSummary> executionSummaries)
    {
        var stringBuilder = new StringBuilder();

        // Add document header
        stringBuilder.AppendLine("# 📊 Performance Test Results");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");


        // Optional: seed used for deterministic randomization (session-level)
        try
        {
            var manifestForSeed = _manifestProvider?.Current;
            var seed = manifestForSeed?.Randomization?.Seed;
            if (seed.HasValue)
            {
                stringBuilder.AppendLine($"Seed: {seed.Value}");
            }
        }
        catch { /* best-effort */ }

        // Optional: timer calibration summary from manifest (session-level)
        try
        {
            var manifest = _manifestProvider?.Current;
            var t = manifest?.TimerCalibration;
            if (t is not null)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("## ⏱️ Timer Calibration");
                stringBuilder.AppendLine($"- freq={t.StopwatchFrequency} Hz, res≈{t.ResolutionNs:F0} ns, baseline={t.MedianTicks} ticks");
                stringBuilder.AppendLine($"- Jitter: RSD={t.RsdPercent:F1}% | Score={t.JitterScore}/100 | N={t.Samples} (warmup {t.Warmups})");
            }
        }
        catch { /* best-effort */ }

        stringBuilder.AppendLine();

        var allExceptions = new List<Exception>();
        var hasResults = false;

        foreach (var result in executionSummaries)
        {
            if (result.CompiledTestCaseResults.Any())
            {
                hasResults = true;
                AppendEnhancedResults(result.TestClass.Name, result.CompiledTestCaseResults, stringBuilder);
                allExceptions.AddRange(result.CompiledTestCaseResults.Where(x => x.Exception is not null).Select(x => x.Exception).Cast<Exception>().ToList());
            }
        }

        if (!hasResults)
        {
            stringBuilder.AppendLine("No performance test results available.");
        }

        AppendExceptions(allExceptions, stringBuilder);

        return stringBuilder.ToString();
    }

    private void AppendEnhancedResults(string typeName, IEnumerable<ICompiledTestCaseResult> compiledResults, StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"## 🧪 {typeName}");
        stringBuilder.AppendLine();

        foreach (var group in compiledResults.GroupBy(x => x.GroupingId))
        {
            if (group.Key is null) continue;

            var groupResults = group.ToList();
            var n = groupResults.Select(x => x.PerformanceRunResult?.SampleSize).Distinct().Single();
            if (n is null or 0) continue;

            // Add group header if there are multiple groups
            if (groupResults.Count > 1 || !string.IsNullOrEmpty(group.Key))
            {
                stringBuilder.AppendLine($"### 📈 {group.Key}");
                stringBuilder.AppendLine();
            }

            // Add performance summary if unified formatter is available
            var performanceSummary = CreatePerformanceSummary(groupResults);
            if (!string.IsNullOrEmpty(performanceSummary))
            {
                stringBuilder.AppendLine(performanceSummary);
                stringBuilder.AppendLine();
            }

            // Add detailed results table (shared magnitude-aware unit so fast tests aren't 0.000ms)
            var unit = SelectGroupUnit(groupResults);
            var unitLabel = DurationFormatter.UnitLabel(unit);
            var table = BuildStatsTable(typeName, groupResults, n.Value, unit);

            stringBuilder.AppendLine(table);
            stringBuilder.AppendLine();
            // Add CI summary lines for each test in the group (95% and 99% if available)
            foreach (var r in groupResults.Where(gr => gr.PerformanceRunResult != null))
            {
                var pr = r.PerformanceRunResult!;
                if (pr.ConfidenceIntervals != null && pr.ConfidenceIntervals.Count > 0)
                {
                    var ciParts = pr.ConfidenceIntervals
                        .OrderBy(ci => ci.ConfidenceLevel)
                        .Select(ci => $"{ci.ConfidenceLevel:P0} CI ± {DurationFormatter.FormatAdaptive(ci.MarginOfError, unit)} {unitLabel}");
                    stringBuilder.AppendLine($"- {r.TestCaseId!.DisplayName}: {string.Join(", ", ciParts)}");
                }
                else
                {
                    stringBuilder.AppendLine($"- {r.TestCaseId!.DisplayName}: {pr.ConfidenceLevel:P0} CI ± {DurationFormatter.FormatAdaptive(pr.MarginOfError, unit)} {unitLabel}");
                }
            }
            stringBuilder.AppendLine();

            // Box-and-whisker plot of every method in the group on a shared axis (same unit as the table)
            AppendGroupDistributionPlot(groupResults, unit, stringBuilder);

            // Append statistical validation warnings (if any)
            foreach (var r in groupResults.Where(gr => gr.PerformanceRunResult?.Validation?.HasWarnings == true))
            {
                var pr = r.PerformanceRunResult!;
                stringBuilder.AppendLine($"- ⚠️ {r.TestCaseId!.DisplayName} warnings:");
                foreach (var w in pr.Validation!.Warnings)
                {
                    var tag = w.Severity switch { ValidationSeverity.Critical => "❗", ValidationSeverity.Warning => "⚠️", _ => "ℹ️" };
                    stringBuilder.AppendLine($"  - {tag} {w.Message}");
                }
                stringBuilder.AppendLine();
            }

        }
    }

    // Renders one box-and-whisker per method in the group, fenced so monospace alignment survives
    // Markdown rendering. Gated by IRunSettings.EnableDistributionPlots (default on).
    private void AppendGroupDistributionPlot(IReadOnlyList<ICompiledTestCaseResult> groupResults, DurationUnit unit, StringBuilder stringBuilder)
    {
        if (!(_runSettings?.EnableDistributionPlots ?? true)) return;

        var series = groupResults
            .Where(r => r.PerformanceRunResult is { } pr && pr.DataWithOutliersRemoved.Length > 0)
            .Select(r =>
            {
                var pr = r.PerformanceRunResult!;
                return BoxPlotData.FromSamples(
                    r.TestCaseId!.DisplayName,
                    pr.DataWithOutliersRemoved,
                    pr.Mean,
                    pr.UpperOutliers.Concat(pr.LowerOutliers).ToArray());
            })
            .ToList();

        var plot = AsciiBoxPlotRenderer.Render(series, unit);
        if (string.IsNullOrEmpty(plot)) return;

        stringBuilder.AppendLine("**Distribution**");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("```text");
        stringBuilder.Append(plot);
        stringBuilder.AppendLine("```");
        stringBuilder.AppendLine();
    }

    private static string CreatePerformanceSummary(List<ICompiledTestCaseResult> groupResults)
    {
        if (groupResults.Count < 2) return string.Empty;

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("**📊 Performance Summary:**");
        stringBuilder.AppendLine();

        // Find fastest and slowest methods
        var validResults = groupResults.Where(r => r.PerformanceRunResult != null).ToList();
        if (validResults.Count < 2) return string.Empty;

        var fastest = validResults.OrderBy(r => r.PerformanceRunResult!.Mean).First();
        var slowest = validResults.OrderByDescending(r => r.PerformanceRunResult!.Mean).First();

        if (fastest.TestCaseId?.DisplayName != slowest.TestCaseId?.DisplayName)
        {
            var speedDifference = (slowest.PerformanceRunResult!.Mean - fastest.PerformanceRunResult!.Mean) / fastest.PerformanceRunResult!.Mean * 100;

            stringBuilder.AppendLine($"- 🟢 **Fastest:** {fastest.TestCaseId?.DisplayName} ({DurationFormatter.FormatAuto(fastest.PerformanceRunResult!.Mean, Decimals)})");
            stringBuilder.AppendLine($"- 🔴 **Slowest:** {slowest.TestCaseId?.DisplayName} ({DurationFormatter.FormatAuto(slowest.PerformanceRunResult!.Mean, Decimals)})");
            stringBuilder.AppendLine($"- 📈 **Performance Gap:** {speedDifference:F1}% difference");
        }

        return stringBuilder.ToString();
    }

    public string ConvertScaleFishResultToMarkdown(IEnumerable<ScalefishClassModel> testClassComplexityResultsEnumerable)
    {
        var testClassComplexityResults = testClassComplexityResultsEnumerable.ToList();
        var tableBuilder = new StringBuilder();
        foreach (var testClassComplexityResult in testClassComplexityResults)
        {
            tableBuilder.AppendLine($"Namespace: {testClassComplexityResult.NameSpace}");
            tableBuilder.AppendLine($"Test Class: {testClassComplexityResult.TestClassName}");
            tableBuilder.AppendLine();
            var methodGroups = testClassComplexityResult
                .ScaleFishMethodModels
                .GroupBy(x => x.TestMethodName);

            foreach (var methodGroup in methodGroups)
            {
                tableBuilder.AppendLine($"### {methodGroup.Key}");
                tableBuilder.AppendLine();
                tableBuilder.AppendLine(methodGroup
                    .SelectMany(x => x.ScaleFishPropertyModels)
                    .ToStringTable(
                        new List<string>
                        {
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            ""
                        },
                        new List<string>
                        {
                            "Variable",
                            "BestFit",
                            "BigO",
                            "GoodnessOfFit",
                            "NextBest",
                            "NextBigO",
                            "NextBestGoodnessOfFit",
                            "DeltaAICc",
                            "AkaikeWeight",
                            "Distinguishable",
                            "ContinuousExponent",
                            "SuggestNextN",
                            "CvStability"
                        },
                        c => c.PropertyName,
                        c => c.ScaleFishModel.ScaleFishModelFunction.Name,
                        c => c.ScaleFishModel.ScaleFishModelFunction.OName,
                        c => c.ScaleFishModel.GoodnessOfFit,
                        c => c.ScaleFishModel.NextClosestScaleFishModelFunction.Name,
                        c => c.ScaleFishModel.NextClosestScaleFishModelFunction.OName,
                        c => c.ScaleFishModel.NextClosestGoodnessOfFit,
                        c => FormatDelta(c.ScaleFishModel.DeltaAicc),
                        c => FormatWeight(c.ScaleFishModel.AkaikeWeight),
                        c => c.ScaleFishModel.IsDistinguishable ? "yes" : "no",
                        c => FormatPowerLog(c.ScaleFishModel.PowerLog),
                        c => FormatSuggestion(c.ScaleFishModel.SuggestedNextN),
                        c => FormatCvStability(c.ScaleFishModel.CrossValidation)
                    ));
                tableBuilder.AppendLine();
            }
        }

        return tableBuilder.ToString();
    }

    private static void AppendResults(string typeName, IEnumerable<ICompiledTestCaseResult> compiledResults, StringBuilder stringBuilder)
    {
        foreach (var group in compiledResults.GroupBy(x => x.GroupingId))
        {
            if (group.Key is null) continue;
            stringBuilder.AppendLine();
            var n = group.Select(x => x.PerformanceRunResult?.SampleSize).Distinct().Single();
            if (n is null or 0) continue;

            var groupResults = group.ToList();
            var unit = SelectGroupUnit(groupResults);
            var table = BuildStatsTable(typeName, groupResults, n.Value, unit);

            stringBuilder.AppendLine(table);

            // Append statistical validation warnings (if any)
            foreach (var r in group.Where(gr => gr.PerformanceRunResult?.Validation?.HasWarnings == true))
            {
                var pr = r.PerformanceRunResult!;
                stringBuilder.AppendLine($"- ⚠️ {r.TestCaseId!.DisplayName} warnings:");
                foreach (var w in pr.Validation!.Warnings)
                {
                    var tag = w.Severity switch { ValidationSeverity.Critical => "❗", ValidationSeverity.Warning => "⚠️", _ => "ℹ️" };
                    stringBuilder.AppendLine($"  - {tag} {w.Message}");
                }
            }

        }
    }

    private static void AppendExceptions(IReadOnlyCollection<Exception?> exceptions, StringBuilder stringBuilder)
    {
        if (exceptions.Count > 0) stringBuilder.AppendLine(" ---- One or more Exceptions encountered ---- ");

        foreach (var exception in exceptions.Where(exception => exception is not null))
        {
            stringBuilder.AppendLine($"Exception: {exception?.Message}\r");
            if (exception?.StackTrace is not null) stringBuilder.AppendLine($"StackTrace:\r{exception.StackTrace}\r");
        }


    }

    private static string FormatDelta(double delta)
    {
        if (double.IsNaN(delta)) return "n/a";
        if (double.IsInfinity(delta)) return "∞";
        return delta.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatWeight(double weight)
    {
        if (double.IsNaN(weight)) return "n/a";
        return weight.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatPowerLog(Sailfish.Analysis.ScaleFish.CurveFitting.PowerLogResult? powerLog)
    {
        return powerLog is null ? "n/a" : powerLog.Describe();
    }

    private static string FormatSuggestion(int? suggested)
    {
        return suggested is null ? "—" : suggested.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatCvStability(Sailfish.Analysis.ScaleFish.CurveFitting.CrossValidationDiagnostic? cv)
    {
        if (cv is null) return "n/a";
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "agree={0:F2} k={1}", cv.RankAgreement, cv.FoldCount);
    }
}