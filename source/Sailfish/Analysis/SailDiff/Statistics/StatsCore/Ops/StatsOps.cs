using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

public static partial class InternalOps
{
    private static double StandardDeviation(this double[] values, double mean, bool unbiased = true)
    {
        return Math.Sqrt(values.Variance(mean, unbiased));
    }

    public static double Variance(this double[] values)
    {
        return values.Variance(values.Mean());
    }

    public static double Variance(this double[] values, double mean)
    {
        return values.Variance(mean, true);
    }

    private static double Variance(this IReadOnlyCollection<double> values, double mean, bool unbiased)
    {
        var a = values.Select(t => t - mean).Select(b => b * b).Sum();
        return unbiased ? a / (values.Count - 1) : a / values.Count;
    }
}