using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Sailfish.MathOps;

public static class ComputeQuartiles
{
    public const string Upper = "Upper";
    public const string Lower = "Lower";

    public static Dictionary<string, int> GetInterQuartileBounds(double[] values)
    {
        values.ToImmutableSortedSet();
        var medianIndex = Median.ComputeMedianIndex(values);
        var lowerQuartileLimit = Median.ComputeMedianIndex(values.Take(medianIndex).ToArray()) + 1;
        var upperQuartileLimit = medianIndex + Median.ComputeMedianIndex(values.Skip(medianIndex).ToArray());
        return new Dictionary<string, int>
        {
            { Upper, upperQuartileLimit },
            { Lower, lowerQuartileLimit }
        };
    }

    public static Quartiles GetQuartiles(double[] values) =>
        new(GetInnerQuartileValues(values), GetOuterQuartileValues(values));

    public static double[] GetInnerQuartileValues(double[] values)
    {
        Array.Sort(values);
        var bounds = GetInterQuartileBounds(values);
        var interQuartileValues = values[(bounds[Lower] - 1)..bounds[Upper]];
        return interQuartileValues;
    }

    public static double[] GetOuterQuartileValues(double[] values)
    {
        Array.Sort(values);
        var bounds = GetInterQuartileBounds(values);
        var outerQuartileValues = values[..(bounds[Lower] - 1)].Concat(values[bounds[Upper]..]).ToArray();
        return outerQuartileValues;
    }
}