using System;
using System.Collections.Generic;

namespace Sailfish.Utils.MathOps;

public static class Median
{
    public static double ComputeMedian(double[] values) =>
        values.Length % 2 == 0 ? ComputeEvenMedian(values) : ComputeOddMedian(values);

    public static int ComputeMedianIndex(double[] values) =>
        values.Length % 2 == 0 ? ComputeEvenMedianIndex(values) : ComputeOddMedianIndex(values);

    private static int ComputeEvenMedianIndex(IReadOnlyCollection<double> values) => values.Count / 2;

    private static int ComputeOddMedianIndex(IReadOnlyCollection<double> values) => (values.Count + 1) / 2;

    private static double ComputeEvenMedian(double[] values)
    {
        Array.Sort(values);
        var leftMiddle = ComputeEvenMedianIndex(values) - 1;
        var a = values[leftMiddle];
        var b = values[leftMiddle + 1];
        return (a + b) / 2;
    }

    private static double ComputeOddMedian(double[] values)
    {
        Array.Sort(values);
        var middle = ComputeOddMedianIndex(values) - 1;
        return values[middle];
    }
}