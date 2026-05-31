using MathNet.Numerics.Statistics;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Formatting;

/// <summary>
/// Full-precision mean/median (in milliseconds) for a SailDiff comparison, recomputed from the raw
/// sample arrays carried on <see cref="StatisticalTestResult"/>.
/// <para>
/// The scalar <c>Mean*/Median*</c> on a <see cref="StatisticalTestResult"/> are pre-rounded to
/// <c>SailDiffSettings.Round</c> decimals (in ms) by the statistical tests, which collapses
/// sub-microsecond values to <c>0.000</c> before any formatter runs. <c>RawDataBefore</c>/
/// <c>RawDataAfter</c> hold the unrounded samples, so their mean/median equal the true values.
/// Recomputing here keeps the display honest without touching the statistics layer or any
/// persisted output (CSV / tracking remain byte-identical).
/// </para>
/// </summary>
public readonly struct SailDiffDisplayStatistics
{
    private SailDiffDisplayStatistics(double meanBefore, double meanAfter, double medianBefore, double medianAfter)
    {
        MeanBefore = meanBefore;
        MeanAfter = meanAfter;
        MedianBefore = medianBefore;
        MedianAfter = medianAfter;
    }

    public double MeanBefore { get; }
    public double MeanAfter { get; }
    public double MedianBefore { get; }
    public double MedianAfter { get; }

    /// <summary>
    /// Builds display statistics from a test result, preferring the full-precision raw arrays and
    /// falling back to the (pre-rounded) scalar values when the raw data is unavailable — e.g. a
    /// failed test or a hand-constructed result.
    /// </summary>
    public static SailDiffDisplayStatistics From(StatisticalTestResult statistics)
    {
        return new SailDiffDisplayStatistics(
            MeanOrFallback(statistics.RawDataBefore, statistics.MeanBefore),
            MeanOrFallback(statistics.RawDataAfter, statistics.MeanAfter),
            MedianOrFallback(statistics.RawDataBefore, statistics.MedianBefore),
            MedianOrFallback(statistics.RawDataAfter, statistics.MedianAfter));
    }

    private static double MeanOrFallback(double[]? rawData, double fallback)
        => rawData is { Length: > 0 } ? rawData.Mean() : fallback;

    private static double MedianOrFallback(double[]? rawData, double fallback)
        => rawData is { Length: > 0 } ? rawData.Median() : fallback;
}
