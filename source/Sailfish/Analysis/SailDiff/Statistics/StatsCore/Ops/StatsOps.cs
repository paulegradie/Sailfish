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

    private static double Variance(this double[] values, double mean, bool unbiased = true)
    {
        var num1 = values.Select(t => t - mean).Select(num2 => num2 * num2).Sum();

        return unbiased ? num1 / (values.Length - 1) : num1 / values.Length;
    }

    public static T Mode<T>(this T[] values) where T : notnull
    {
        return values.Mode(out _, false);
    }

    private static T Mode<T>(this T[] values, out int count, bool inPlace, bool alreadySorted = false) where T : notnull
    {
        if (values.Length == 0)
            throw new ArgumentException("The values vector cannot be empty.", nameof(values));
        return (object)values[0] is IComparable ? mode_sort(values, inPlace, alreadySorted, out count) : mode_bag(values, out count);
    }

    private static T mode_bag<T>(IReadOnlyList<T> values, out int bestCount) where T : notnull
    {
        var obj = values[0];
        bestCount = 1;
        var dictionary = new Dictionary<T, int>();
        foreach (var key in values)
        {
            if (!dictionary.TryGetValue(key, out var num))
                num = 1;
            else
                ++num;
            dictionary[key] = num;
            if (num <= bestCount) continue;
            bestCount = num;
            obj = key;
        }

        return obj;
    }

    private static T mode_sort<T>(T[] values, bool inPlace, bool alreadySorted, out int bestCount)
    {
        if (!alreadySorted)
        {
            if (!inPlace)
                values = (T[])values.Clone();
            Array.Sort(values);
        }

        var obj1 = values[0];
        var num = 1;
        var obj2 = obj1;
        bestCount = num;
        for (var index = 1; index < values.Length; ++index)
        {
            if (obj1.Equals(values[index]))
            {
                ++num;
            }
            else
            {
                obj1 = values[index];
                num = 1;
            }

            if (num <= bestCount) continue;
            bestCount = num;
            obj2 = obj1;
        }

        return obj2;
    }
}