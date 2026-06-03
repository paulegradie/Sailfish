using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Diagnostics.Environment;
using Sailfish.Results;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.SailDiff.Formatting;

namespace Sailfish.Analysis.Ai;

internal interface IPerformanceNarrativeContextBuilder
{
    PerformanceNarrativeContext Build(SailDiffAnalysisCompleteNotification notification, double alpha);

    PerformanceNarrativeContext BuildScaling(ScaleFishAnalysisCompleteNotification notification);
}

/// <summary>
///     Lifts the authoritative SailDiff figures into the grounded packet the agent reasons over. The verdict is
///     derived here (deterministically, from the p-value and the direction of the mean shift) so the agent never
///     has to — and so the same vocabulary is used everywhere.
/// </summary>
internal sealed class PerformanceNarrativeContextBuilder : IPerformanceNarrativeContextBuilder
{
    private readonly IEnvironmentHealthReportProvider healthProvider;
    private readonly IReproducibilityManifestProvider manifestProvider;

    public PerformanceNarrativeContextBuilder(
        IReproducibilityManifestProvider manifestProvider,
        IEnvironmentHealthReportProvider healthProvider)
    {
        this.manifestProvider = manifestProvider;
        this.healthProvider = healthProvider;
    }

    public PerformanceNarrativeContext Build(SailDiffAnalysisCompleteNotification notification, double alpha)
    {
        var comparisons = notification.TestCaseResults
            .Select(result => ToCaseContext(result, alpha))
            .ToList();

        return new PerformanceNarrativeContext(comparisons, notification.ResultsAsMarkdown ?? string.Empty, BuildEnvironment());
    }

    public PerformanceNarrativeContext BuildScaling(ScaleFishAnalysisCompleteNotification notification)
    {
        var verdicts = new List<ComplexityVerdict>();
        foreach (var classModel in notification.TestClassComplexityResults)
        foreach (var method in classModel.ScaleFishMethodModels)
        foreach (var property in method.ScaleFishPropertyModels)
        {
            var model = property.ScaleFishModel;
            verdicts.Add(new ComplexityVerdict(
                method.TestMethodName,
                property.PropertyName,
                model.ScaleFishModelFunction.OName,
                model.GoodnessOfFit,
                model.NextClosestScaleFishModelFunction.OName,
                model.NextClosestGoodnessOfFit,
                model.IsDistinguishable,
                model.SuggestedNextN,
                Project(model.ScaleFishModelFunction)));
        }

        return new PerformanceNarrativeContext(
            System.Array.Empty<SailDiffCaseContext>(),
            notification.ScaleFishResultMarkdown ?? string.Empty,
            BuildEnvironment(),
            verdicts);
    }

    private static IReadOnlyList<ComplexityProjection> Project(ScaleFishModelFunction function)
    {
        var projections = new List<ComplexityProjection>();
        foreach (var n in new[] { 100, 1_000, 10_000 })
        {
            try
            {
                projections.Add(new ComplexityProjection(n, function.Predict(n)));
            }
            catch
            {
                // Family not fit, or evaluation overflowed at this N — skip the projection rather than fail.
            }
        }

        return projections;
    }

    /// <summary>
    ///     Projects the reproducibility manifest and environment health report (if captured) into a concise
    ///     snapshot. Returns null when neither is available — the narrative simply proceeds without environment
    ///     context. Both are read defensively so timing of capture never breaks the analysis.
    /// </summary>
    private EnvironmentSnapshot? BuildEnvironment()
    {
        var manifest = manifestProvider.Current;
        var health = healthProvider.Current;
        if (manifest is null && health is null) return null;

        var concerns = health?.Entries
            .Where(entry => entry.Status is HealthStatus.Warn or HealthStatus.Fail)
            .Select(entry => new EnvironmentConcern(entry.Name, entry.Status.ToString(), entry.Details, entry.Recommendation))
            .ToList() ?? new List<EnvironmentConcern>();

        return new EnvironmentSnapshot(
            manifest?.DotNetRuntime ?? string.Empty,
            manifest?.Os ?? string.Empty,
            manifest?.OsArchitecture ?? string.Empty,
            manifest?.ProcessArchitecture ?? string.Empty,
            manifest?.CpuModel,
            manifest?.GcMode ?? string.Empty,
            manifest?.Jit ?? string.Empty,
            manifest?.CpuAffinity ?? string.Empty,
            manifest?.Timer ?? string.Empty,
            manifest?.EnvironmentHealthScore ?? health?.Score ?? 0,
            manifest?.EnvironmentHealthLabel ?? health?.SummaryLabel,
            manifest?.CiSystem,
            manifest?.CommitSha,
            concerns);
    }

    private static SailDiffCaseContext ToCaseContext(SailDiffResult result, double alpha)
    {
        var displayName = result.TestCaseId.DisplayName;
        var stats = result.TestResultsWithOutlierAnalysis.StatisticalTestResult;

        if (stats.Failed)
        {
            return new SailDiffCaseContext(
                displayName, SkipperVerdict.Inconclusive,
                MeanBefore: 0, MeanAfter: 0, MedianBefore: 0, MedianAfter: 0,
                PercentChangeMean: 0, PValue: double.NaN, AdjustedPValue: null,
                ChangeDescription: stats.ChangeDescription, SampleSizeBefore: 0, SampleSizeAfter: 0, Failed: true);
        }

        // Use full-precision means/medians recomputed from the raw samples. The scalar
        // Mean*/Median* on StatisticalTestResult are pre-rounded to SailDiffSettings.Round decimals
        // (ms), which collapses sub-millisecond values to ~0.001 — that zeroes the percent-change,
        // flips the verdict direction when both means round equal, and feeds the agent rounded
        // figures that disagree with the embedded results markdown.
        var display = SailDiffDisplayStatistics.From(stats);

        var percentChangeMean = display.MeanBefore != 0
            ? (display.MeanAfter - display.MeanBefore) / display.MeanBefore * 100.0
            : 0.0;

        return new SailDiffCaseContext(
            displayName,
            DeriveVerdict(display, stats, alpha),
            display.MeanBefore,
            display.MeanAfter,
            display.MedianBefore,
            display.MedianAfter,
            percentChangeMean,
            stats.PValue,
            stats.QValue,
            stats.ChangeDescription,
            stats.SampleSizeBefore,
            stats.SampleSizeAfter,
            Failed: false,
            EffectSizeName: stats.EffectSize?.Name,
            EffectSizeValue: stats.EffectSize?.Value,
            MinimumDetectableEffectPercent: stats.MinimumDetectableEffectPercent);
    }

    private static SkipperVerdict DeriveVerdict(SailDiffDisplayStatistics display, StatisticalTestResult stats, double alpha)
    {
        // Prefer the BH-FDR adjusted q-value when present (it controls the family-wise error rate across the
        // pairs in an N×N method comparison); otherwise fall back to the raw p-value.
        var p = stats.QValue ?? stats.PValue;
        if (double.IsNaN(p) || p >= alpha) return SkipperVerdict.NotSignificant;

        // Direction from the full-precision means (see ToCaseContext): the rounded scalars can
        // collapse a real sub-resolution change to equal and mislabel the direction.
        return display.MeanAfter > display.MeanBefore ? SkipperVerdict.Regressed : SkipperVerdict.Improved;
    }
}
