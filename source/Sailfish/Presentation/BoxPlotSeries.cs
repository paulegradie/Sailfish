using System;
using System.Collections.Generic;

namespace Sailfish.Presentation;

/// <summary>
/// Immutable five-number summary (plus mean and removed outliers) for one distribution, ready to be
/// drawn as a box-and-whisker plot. All durations are in <b>milliseconds</b> — Sailfish stores every
/// measurement in ms (see <c>PerformanceRunResult.ConvertFromPerfTimer</c>) and the display unit is
/// applied only at render time, exactly like <see cref="DurationFormatter"/>.
/// <para>
/// <see cref="Outliers"/> are the points Sailfish's own detector already removed from the sample; the
/// box and whiskers are computed from the <em>cleaned</em> data, so the whiskers span the cleaned
/// min–max and the outliers are drawn as separate markers. When a surface has no separate outlier
/// list (the SailDiff comparison arrays are already cleaned), pass an empty list.
/// </para>
/// </summary>
public sealed record BoxPlotSeries(
    string Label,
    double Min,
    double Q1,
    double Median,
    double Q3,
    double Max,
    double Mean,
    IReadOnlyList<double> Outliers,
    int N)
{
    /// <summary>True when there is no finite data to plot.</summary>
    public bool IsEmpty => N == 0;

    /// <summary>True when every value coincides (zero spread) — render as a single marker.</summary>
    public bool HasNoSpread => N > 0 && Max - Min <= 0;

    /// <summary>Number of outliers Sailfish removed before computing this summary.</summary>
    public int OutlierCount => Outliers?.Count ?? 0;
}
