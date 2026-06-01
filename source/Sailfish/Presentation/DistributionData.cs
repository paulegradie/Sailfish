using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Presentation;

/// <summary>
/// One distribution to plot as a histogram row: a label, the in-range (outlier-removed) samples in
/// <b>milliseconds</b>, the mean and median (also ms), and how many outliers were removed. The
/// renderer derives n / min / max from <see cref="Samples"/>; mean and median are supplied because
/// Sailfish (and SailDiff) already compute them.
/// </summary>
public sealed record DistributionData(
    string Label,
    IReadOnlyList<double> Samples,
    double Mean,
    double Median,
    int OutlierCount)
{
    public bool IsEmpty => Samples is null || Samples.Count == 0;

    /// <summary>
    /// Builds a distribution from raw samples, filtering non-finite values and recomputing mean/median
    /// from the samples when the supplied values are not finite.
    /// </summary>
    public static DistributionData FromSamples(string label, IReadOnlyList<double> samples, double mean, double median, int outlierCount)
    {
        var finite = (samples ?? Array.Empty<double>()).Where(double.IsFinite).ToArray();
        if (finite.Length == 0)
        {
            return new DistributionData(label, finite, double.NaN, double.NaN, Math.Max(0, outlierCount));
        }

        var resolvedMean = double.IsFinite(mean) ? mean : finite.Average();
        var resolvedMedian = double.IsFinite(median) ? median : ComputeMedian(finite);
        return new DistributionData(label, finite, resolvedMean, resolvedMedian, Math.Max(0, outlierCount));
    }

    private static double ComputeMedian(double[] values)
    {
        var sorted = (double[])values.Clone();
        Array.Sort(sorted);
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }
}
