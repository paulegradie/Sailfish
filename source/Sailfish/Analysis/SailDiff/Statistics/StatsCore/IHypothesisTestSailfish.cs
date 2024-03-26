using System;
using System.Globalization;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public abstract class HypothesisTest<TDist> : IFormattable
{
    public double PValue { get; protected set; }

    public double Statistic { get; protected set; }

    public double Size => 0.05;

    public string ToString(string? format, IFormatProvider? formatProvider) => PValue.ToString(format, formatProvider);

    public DistributionTailSailfish Tail { get; protected set; }
    public bool Significant { get; }

    public abstract double StatisticToPValue(double x);

    public abstract double PValueToStatistic(double p);

    public abstract TDist StatisticDistribution { get; set; }

    public override string ToString()
    {
        return PValue.ToString(CultureInfo.CurrentCulture);
    }
}