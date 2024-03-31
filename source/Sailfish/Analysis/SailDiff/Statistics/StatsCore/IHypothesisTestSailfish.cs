using System;
using System.Globalization;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public abstract class HypothesisTest : IFormattable
{
    public double PValue { get; protected init; }

    public double Statistic { get; protected init; }

    public double Size => 0.05;

    public string ToString(string? format, IFormatProvider? formatProvider) => PValue.ToString(format, formatProvider);

    public DistributionTailSailfish Tail { get; protected init; }

    public override string ToString()
    {
        return PValue.ToString(CultureInfo.CurrentCulture);
    }
}