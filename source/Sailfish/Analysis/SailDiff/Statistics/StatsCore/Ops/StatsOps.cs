using System;
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

    private static double Variance(this double[] values, double mean, bool unbiased = true)
    {
        var num1 = values.Select(t => t - mean).Select(num2 => num2 * num2).Sum();

        return unbiased ? num1 / (values.Length - 1) : num1 / values.Length;
    }
}