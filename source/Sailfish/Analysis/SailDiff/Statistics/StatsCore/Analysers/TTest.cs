using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

public static class TTestExtensionMethods
{
    public static double StatisticToPValue(
        double t,
        TDistribution distribution,
        DistributionTailSailfish type)
    {
        switch (type)
        {
            case DistributionTailSailfish.TwoTail:
                return 2.0 * distribution.ComplementaryDistributionFunction(Math.Abs(t));

            case DistributionTailSailfish.OneUpper:
                return distribution.ComplementaryDistributionFunction(t);

            case DistributionTailSailfish.OneLower:
                return distribution.DistributionFunction(t);

            default:
                throw new InvalidOperationException();
        }
    }

    public static double PValueToStatistic(
        double p,
        TDistribution distribution,
        DistributionTailSailfish type)
    {
        switch (type)
        {
            case DistributionTailSailfish.TwoTail:
                return distribution.InverseDistributionFunction(1.0 - p / 2.0);

            case DistributionTailSailfish.OneUpper:
                return distribution.InverseDistributionFunction(1.0 - p);

            case DistributionTailSailfish.OneLower:
                return distribution.InverseDistributionFunction(p);

            default:
                throw new InvalidOperationException();
        }
    }
}