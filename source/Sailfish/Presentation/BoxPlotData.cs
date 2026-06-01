using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace Sailfish.Presentation;

/// <summary>
/// Builds <see cref="BoxPlotSeries"/> five-number summaries from raw samples. Quartiles use the same
/// MathNet.Numerics statistics the rest of Sailfish already relies on (<c>Mean()</c>/<c>Median()</c>
/// on <c>PerformanceRunResult</c>), so a box plot is always consistent with the scalar stats shown
/// next to it.
/// </summary>
public static class BoxPlotData
{
    /// <summary>
    /// Builds a series from an already-cleaned sample (outliers removed). The whiskers span the
    /// cleaned min–max; <paramref name="removedOutliers"/> — Sailfish's own detected outliers — are
    /// carried through to be drawn as separate markers. Non-finite values are filtered out.
    /// </summary>
    /// <param name="label">Row label (method / test-case / group member name).</param>
    /// <param name="cleaned">The sample after Sailfish outlier removal, in milliseconds.</param>
    /// <param name="mean">The mean Sailfish already computed (in ms); recomputed from
    /// <paramref name="cleaned"/> only if this is non-finite.</param>
    /// <param name="removedOutliers">Outliers Sailfish removed, in ms. Optional.</param>
    public static BoxPlotSeries FromSamples(
        string label,
        IReadOnlyList<double> cleaned,
        double mean,
        IReadOnlyList<double>? removedOutliers = null)
    {
        var finite = (cleaned ?? Array.Empty<double>()).Where(double.IsFinite).ToArray();
        var outliers = (removedOutliers ?? Array.Empty<double>()).Where(double.IsFinite).ToArray();

        if (finite.Length == 0)
        {
            return new BoxPlotSeries(label, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, outliers, 0);
        }

        if (finite.Length == 1)
        {
            var only = finite[0];
            return new BoxPlotSeries(label, only, only, only, only, only, double.IsFinite(mean) ? mean : only, outliers, 1);
        }

        var min = finite.Minimum();
        var max = finite.Maximum();
        var q1 = finite.LowerQuartile();
        var median = finite.Median();
        var q3 = finite.UpperQuartile();
        var resolvedMean = double.IsFinite(mean) ? mean : finite.Mean();

        return new BoxPlotSeries(label, min, q1, median, q3, max, resolvedMean, outliers, finite.Length);
    }

    /// <summary>
    /// Convenience overload that computes the mean from <paramref name="cleaned"/>.
    /// </summary>
    public static BoxPlotSeries FromSamples(string label, IReadOnlyList<double> cleaned, IReadOnlyList<double>? removedOutliers = null)
        => FromSamples(label, cleaned, double.NaN, removedOutliers);
}
