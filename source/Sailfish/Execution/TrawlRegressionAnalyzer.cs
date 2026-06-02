using System;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Execution;

/// <summary>
///     Compares a Trawl run's latency distribution against a baseline using SailDiff's statistical test
///     executor — the exact same significance machinery the microbenchmark path uses, reused at the raw
///     <c>double[]</c> seam. The verdict follows the project's comparison vocabulary: a significant slowdown
///     is "Current is N% slower than baseline", otherwise "NOT SIGNIFICANT".
/// </summary>
internal sealed class TrawlRegressionAnalyzer
{
    private readonly IStatisticalTestExecutor _executor;

    public TrawlRegressionAnalyzer(IStatisticalTestExecutor executor)
    {
        _executor = executor;
    }

    public TrawlRegressionVerdict Compare(double[] baselineLatenciesMs, double[] currentLatenciesMs, SailDiffSettings settings)
    {
        if (baselineLatenciesMs is null || currentLatenciesMs is null || baselineLatenciesMs.Length == 0 || currentLatenciesMs.Length == 0)
            return new TrawlRegressionVerdict { Outcome = TrawlRegressionOutcome.Inconclusive, Message = "No baseline or current samples to compare." };

        TestResultWithOutlierAnalysis result;
        try
        {
            // before = baseline, after = current.
            result = _executor.ExecuteStatisticalTest(baselineLatenciesMs, currentLatenciesMs, settings);
        }
        catch (Exception ex)
        {
            return new TrawlRegressionVerdict { Outcome = TrawlRegressionOutcome.Inconclusive, Message = $"Comparison failed: {ex.Message}" };
        }

        var stat = result.StatisticalTestResult;
        if (stat.Failed)
            return new TrawlRegressionVerdict { Outcome = TrawlRegressionOutcome.Inconclusive, Message = $"Comparison failed: {result.ExceptionMessage}" };

        var before = stat.MeanBefore;
        var after = stat.MeanAfter;
        var percentChange = before > 0 ? (after - before) / before * 100.0 : 0;
        var significant = stat.PValue < settings.Alpha;

        if (!significant)
        {
            return new TrawlRegressionVerdict
            {
                Outcome = TrawlRegressionOutcome.NotSignificant,
                PercentChange = percentChange,
                PValue = stat.PValue,
                Message = $"NOT SIGNIFICANT: current vs baseline mean latency {before:0.##}ms -> {after:0.##}ms (p={stat.PValue:0.####}, alpha={settings.Alpha})"
            };
        }

        var slower = after > before;
        return new TrawlRegressionVerdict
        {
            Outcome = slower ? TrawlRegressionOutcome.Regressed : TrawlRegressionOutcome.Improved,
            PercentChange = percentChange,
            PValue = stat.PValue,
            Message = $"Current is {Math.Abs(percentChange):0.##}% {(slower ? "slower" : "faster")} than baseline (mean latency {before:0.##}ms -> {after:0.##}ms, p={stat.PValue:0.####})"
        };
    }
}
