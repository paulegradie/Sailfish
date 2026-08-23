using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Sailfish.Analysis.SailDiff;

/// <summary>
/// A method participating in one comparison cohort (a single variable set within a single comparison
/// group). <see cref="Result"/> carries the raw samples the configured statistical test runs on.
/// </summary>
public sealed record MethodComparisonMember(string Id, string MethodName, bool IsBaseline, PerformanceRunResult Result);

/// <summary>
/// The significance verdict for a contender relative to its primary/baseline. <see cref="Similar"/> means
/// "not significant after BH-FDR" — never silently "no change". Direction is taken from the means.
/// </summary>
public enum MethodComparisonVerdict
{
    Similar,
    Improved,
    Slower
}

/// <summary>
/// One pair's comparison, oriented primary/baseline (before) vs compared/contender (after).
/// Significance (<see cref="PValue"/>/<see cref="QValue"/>/<see cref="Verdict"/>) comes from the configured
/// SailDiff test with one BH-FDR pass over the cohort; <see cref="Ratio"/> + CI is the effect size.
/// </summary>
public sealed record MethodComparisonPairResult(
    MethodComparisonMember Primary,
    MethodComparisonMember Compared,
    double PrimaryMean,
    double ComparedMean,
    double PrimaryMedian,
    double ComparedMedian,
    int PrimarySampleSize,
    int ComparedSampleSize,
    double PValue,
    double QValue,
    double Ratio,
    double? CiLower,
    double? CiUpper,
    MethodComparisonVerdict Verdict);

/// <summary>The unified result for one comparison cohort.</summary>
public sealed record MethodComparisonResult(
    IReadOnlyList<MethodComparisonPairResult> Pairs,
    bool BaselineMode,
    string? BaselineId)
{
    /// <summary>
    /// Looks up the computed pair for an unordered (a, b) method id pair, regardless of which id was the
    /// primary. Returns <c>null</c> when no comparison was produced (e.g. insufficient samples).
    /// </summary>
    public MethodComparisonPairResult? Find(string idA, string idB)
    {
        // Canonicalize ids the same way ComputeTest re-keys its results (TestCaseId.DisplayName appends
        // "()" to a bare name), so the lookup is robust to that round-trip — not just to argument ordering.
        static string Canonicalize(string id) => new TestCaseId(id).DisplayName;
        var key = MultipleComparisons.NormalizePair(Canonicalize(idA), Canonicalize(idB));
        return Pairs.FirstOrDefault(p =>
            MultipleComparisons.NormalizePair(Canonicalize(p.Primary.Id), Canonicalize(p.Compared.Id)) == key);
    }
}

/// <summary>
/// Computes the verdict for a method-vs-method comparison cohort so every surface (IDE, markdown, CSV,
/// console) agrees. Significance is the configured SailDiff test (Wilcoxon Rank-Sum by default) run on the
/// raw samples, with a single Benjamini-Hochberg FDR pass across the cohort's pairs — the exact same engine
/// and multiplicity control historical SailDiff uses — rather than the old per-surface parametric log-ratio
/// approximation. The ratio + confidence interval is reported separately as the effect size.
/// </summary>
public interface IMethodComparisonAnalyzer
{
    MethodComparisonResult Analyze(IReadOnlyList<MethodComparisonMember> members, SailDiffSettings settings);
}

/// <inheritdoc cref="IMethodComparisonAnalyzer" />
public sealed class MethodComparisonAnalyzer : IMethodComparisonAnalyzer
{
    // Smallest positive significance value substituted for an exact-zero p/q from a successful test, so a
    // maximally-separated comparison still reads as significant rather than as "no comparison computed".
    private const double MinPositiveSignificance = 1e-300;

    private readonly IStatisticalTestComputer _statisticalTestComputer;

    public MethodComparisonAnalyzer(IStatisticalTestComputer statisticalTestComputer)
    {
        _statisticalTestComputer = statisticalTestComputer;
    }

    public MethodComparisonResult Analyze(IReadOnlyList<MethodComparisonMember> members, SailDiffSettings settings)
    {
        var usable = members.Where(m => m.Result is { Mean: > 0 }).ToList();
        var baselines = usable.Where(m => m.IsBaseline).ToList();
        var baselineMode = baselines.Count == 1;
        var baselineId = baselines.Count == 1 ? baselines[0].Id : null;

        // Ordered (primary, compared) pairs: baseline-vs-contender when exactly one baseline is marked,
        // otherwise the full N×N (each unordered pair once; the matrix renderer mirrors a pair's cell).
        var pairs = new List<(MethodComparisonMember Primary, MethodComparisonMember Compared)>();
        if (baselineMode)
        {
            var baseline = baselines[0];
            foreach (var contender in usable.Where(m => !ReferenceEquals(m, baseline)))
                pairs.Add((baseline, contender));
        }
        else
        {
            for (var i = 0; i < usable.Count; i++)
            for (var j = i + 1; j < usable.Count; j++)
                pairs.Add((usable[i], usable[j]));
        }

        if (pairs.Count == 0)
            return new MethodComparisonResult(Array.Empty<MethodComparisonPairResult>(), baselineMode, baselineId);

        // Relabel each pair to a unique synthetic id, then run the whole cohort through ONE ComputeTest
        // call. The engine tests each pair with the configured test and applies a single BH-FDR pass
        // (q-values + verdict demotion) across them — identical to historical SailDiff. before = primary,
        // after = compared, so MeanBefore/After line up with primary/compared.
        var pairKeys = new string[pairs.Count];
        var beforeData = new List<PerformanceRunResult>(pairs.Count);
        var afterData = new List<PerformanceRunResult>(pairs.Count);
        for (var i = 0; i < pairs.Count; i++)
        {
            var key = $"__methodcomparison::{i}";
            pairKeys[i] = key;
            beforeData.Add(WithDisplayName(pairs[i].Primary.Result, key));
            afterData.Add(WithDisplayName(pairs[i].Compared.Result, key));
        }

        var statByKey = _statisticalTestComputer
            .ComputeTest(new TestData(pairKeys, beforeData), new TestData(pairKeys, afterData), settings)
            .Where(r => r.TestResultsWithOutlierAnalysis?.StatisticalTestResult is { Failed: false })
            .GroupBy(r => r.TestCaseId.DisplayName)
            .ToDictionary(g => g.Key, g => g.First().TestResultsWithOutlierAnalysis!.StatisticalTestResult);

        var confidenceLevel = 1.0 - settings.Alpha;
        var results = new List<MethodComparisonPairResult>(pairs.Count);
        for (var i = 0; i < pairs.Count; i++)
        {
            var (primary, compared) = pairs[i];
            // ComputeTest re-keys each result by `new TestCaseId(displayName).DisplayName`, which normalizes
            // a bare name to "name()". Normalize the lookup the same way so the key round-trips.
            statByKey.TryGetValue(new TestCaseId(pairKeys[i]).DisplayName, out var stat);

            var primaryMean = primary.Result.Mean;
            var comparedMean = compared.Result.Mean;
            var primaryN = SampleCount(primary.Result);
            var comparedN = SampleCount(compared.Result);

            var (ratio, lower, upper) = MultipleComparisons.ComputeRatioCi(
                primaryMean, StandardError(primary.Result, primaryN), primaryN,
                comparedMean, StandardError(compared.Result, comparedN), comparedN,
                confidenceLevel);

            // Significance is the engine's q-value (BH-FDR adjusted). When a pair could not be tested
            // (e.g. < 3 samples), p/q are NaN and the verdict falls through to Similar.
            //
            // A SUCCESSFUL test with maximal separation (e.g. a method that is comfortably faster than its
            // peer) can return an exact p — and hence BH q — of exactly 0. Zero is otherwise the "no
            // comparison computed" sentinel that SailDiffSignificance.IsSignificantPositive treats as not
            // significant, so without this floor the most clear-cut difference would be mislabelled
            // "Similar". Floor a real 0 to a tiny positive so the verdict stays significant (this mirrors the
            // MinPositivePValue floor MultipleComparisons.LogRatioPValue already applies).
            var rawP = stat?.PValue ?? double.NaN;
            var rawQ = stat?.QValue ?? rawP;
            var pValue = stat is { Failed: false } && rawP == 0.0 ? MinPositiveSignificance : rawP;
            var qValue = stat is { Failed: false } && rawQ == 0.0 ? MinPositiveSignificance : rawQ;

            results.Add(new MethodComparisonPairResult(
                primary, compared,
                primaryMean, comparedMean,
                primary.Result.Median, compared.Result.Median,
                primaryN, comparedN,
                pValue, qValue,
                ratio, lower, upper,
                DetermineVerdict(qValue, settings.Alpha, ratio)));
        }

        return new MethodComparisonResult(results, baselineMode, baselineId);
    }

    // Verdict (sig-after-FDR, then direction from the ratio) lives in MethodComparisonDisplay so the analyzer
    // and every render surface share one definition. ratio = compared / primary, so ratio < 1 ⇒ Improved.
    private static MethodComparisonVerdict DetermineVerdict(double qValue, double alpha, double ratio)
        => MethodComparisonDisplay.Verdict(qValue, alpha, ratio);

    // Effective N: the post-outlier-removal sample count when available, else the raw sample size. Matches
    // the N the surfaces already used for the ratio CI.
    private static int SampleCount(PerformanceRunResult r)
    {
        var cleaned = r.DataWithOutliersRemoved?.Length ?? 0;
        // Floor at 1 (never 0) to match the ratio/CI fallback the markdown/CSV surfaces used before this.
        return cleaned > 0 ? cleaned : Math.Max(1, r.SampleSize);
    }

    private static double StandardError(PerformanceRunResult r, int n)
    {
        return n > 1 && r.StdDev > 0 ? r.StdDev / Math.Sqrt(n) : 0.0;
    }

    // PerformanceRunResult.DisplayName is constructor-set; clone with the synthetic pair id so a single
    // ComputeTest call can pair the two methods (it groups before/after by DisplayName).
    private static PerformanceRunResult WithDisplayName(PerformanceRunResult r, string displayName)
    {
        return new PerformanceRunResult(
            displayName,
            r.Mean, r.StdDev, r.Variance, r.Median,
            r.RawExecutionResults, r.SampleSize, r.NumWarmupIterations,
            r.DataWithOutliersRemoved, r.UpperOutliers, r.LowerOutliers, r.TotalNumOutliers,
            r.StandardError, r.ConfidenceLevel, r.ConfidenceIntervalLower, r.ConfidenceIntervalUpper,
            r.MarginOfError, r.ConfidenceIntervals);
    }
}

/// <summary>
/// Projection helpers between the persisted tracking format and the runtime model so file-based surfaces
/// (console markdown / CSV) can feed the shared <see cref="IMethodComparisonAnalyzer" />.
/// </summary>
public static class PerformanceRunResultTrackingFormatExtensions
{
    /// <summary>
    /// Projects a tracking-format result back to a runtime <see cref="PerformanceRunResult" /> so the shared
    /// statistical engine can run on it. Confidence-interval fields the tracking format does not persist are
    /// left at their defaults — SailDiff recomputes everything it needs from the raw samples.
    /// </summary>
    public static PerformanceRunResult ToPerformanceRunResult(this PerformanceRunResultTrackingFormat t)
    {
        return new PerformanceRunResult(
            t.DisplayName, t.Mean, t.StdDev, t.Variance, t.Median,
            t.RawExecutionResults, t.SampleSize, t.NumWarmupIterations,
            t.DataWithOutliersRemoved, t.UpperOutliers, t.LowerOutliers, t.TotalNumOutliers);
    }
}
