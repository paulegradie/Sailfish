using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using System;
using System.Globalization;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;

internal abstract class HypothesisTest : IFormattable
{
    public double PValue { get; protected init; }

    public double Statistic { get; protected init; }

    public static double Size => 0.05;
    public DistributionTailSailfish Tail { get; protected init; }

    public string ToString(string? format, IFormatProvider? formatProvider) => PValue.ToString(format, formatProvider);


    public override string ToString()
    {
        return PValue.ToString(CultureInfo.CurrentCulture);
    }
}