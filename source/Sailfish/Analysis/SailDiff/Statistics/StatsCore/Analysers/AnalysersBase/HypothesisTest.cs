using System;
using System.Globalization;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;

internal abstract class HypothesisTest : IFormattable
{
    public double PValue { get; protected init; }

    public double Statistic { get; protected init; }

    /// <summary>
    /// Conventional default significance threshold (Type I error). Concrete tests should prefer
    /// the instance <see cref="Size"/> property, which derives from the alpha passed at
    /// construction time and falls back to this default.
    /// </summary>
    public const double DefaultSize = 0.05;

    /// <summary>
    /// The significance threshold (and 1 − confidence level for the reported CI) used by this
    /// test instance. Set from the caller's <see cref="Sailfish.Analysis.SailDiff.SailDiffSettings.Alpha"/>
    /// when available; otherwise <see cref="DefaultSize"/>. Replaces the previous static
    /// <c>Size</c> property whose hardcoded 0.05 ignored the user's configured alpha.
    /// </summary>
    public double Size { get; protected init; } = DefaultSize;

    public DistributionTailSailfish Tail { get; protected init; }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return PValue.ToString(format, formatProvider);
    }


    public override string ToString()
    {
        return PValue.ToString(CultureInfo.CurrentCulture);
    }
}
