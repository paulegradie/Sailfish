using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

/* Unmerged change from project 'Sailfish (net7.0)'
Before:
public static partial class OpsExtensionMethods {
    public static int Sum(this int[] vector) {
After:
public static partial class OpsExtensionMethods {
    public static int Sum(this int[] vector) {
*/

/* Unmerged change from project 'Sailfish (net6.0)'
Before:
public static partial class OpsExtensionMethods {
    public static int Sum(this int[] vector) {
After:
public static partial class OpsExtensionMethods {
    public static int Sum(this int[] vector) {
*/

public static partial class InternalOps
{
    public static int Sum(this int[] vector)
    {
        var num = 0;
        for (var index = 0; index < vector.Length; ++index)
            num += vector[index];
        return num;
    }

    public static double[,] WeightedCovariance(
        this double[][] matrix,
        double[]? weights,
        double[] means,
        int dimension)
    {
        var num1 = 0.0;
        var num2 = 0.0;
        for (var index = 0; index < weights.Length; ++index)
        {
            num1 += weights[index];
            num2 += weights[index] * weights[index];
        }

        var factor = num1 / (num1 * num1 - num2);
        return matrix.WeightedScatter(weights, means, factor, dimension);
    }

    public static double StandardDeviation(this double[] values, double mean, bool unbiased = true)
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

    public static double Variance(this double[] values, double mean, bool unbiased = true)
    {
        var num1 = 0.0;
        for (var index = 0; index < values.Length; ++index)
        {
            var num2 = values[index] - mean;
            num1 += num2 * num2;
        }

        return unbiased ? num1 / (values.Length - 1) : num1 / values.Length;

        /* Unmerged change from project 'Sailfish (net7.0)'
        Before:
            }

            public static T Mode<T>(this T[] values) {
        After:
            }

            public static T Mode<T>(this T[] values) {
        */

        /* Unmerged change from project 'Sailfish (net6.0)'
        Before:
            }

            public static T Mode<T>(this T[] values) {
        After:
            }

            public static T Mode<T>(this T[] values) {
        */
    }

    public static T Mode<T>(this T[] values)
    {
        return values.Mode(out var _, false);
    }

    public static T Mode<T>(this T[] values, out int count, bool inPlace, bool alreadySorted = false)
    {
        if (values.Length == 0)
            throw new ArgumentException("The values vector cannot be empty.", nameof(values));
        return (object)values[0] is IComparable ? mode_sort(values, inPlace, alreadySorted, out count) : mode_bag(values, out count);
    }

    private static T mode_bag<T>(T[] values, out int bestCount)
    {
        var obj = values[0];
        bestCount = 1;
        var dictionary = new Dictionary<T, int>();
        foreach (var key in values)
        {
            int num;
            if (!dictionary.TryGetValue(key, out num))
                num = 1;
            else
                ++num;
            dictionary[key] = num;
            if (num > bestCount)
            {
                bestCount = num;
                obj = key;
            }
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

            if (num > bestCount)
            {
                bestCount = num;
                obj2 = obj1;
            }
        }

        return obj2;
    }

    public static double Entropy(this double[] values, Func<double, double> pdf)
    {
        var num = 0.0;
        for (var index = 0; index < values.Length; ++index)
        {
            var d = pdf(values[index]);
            num += d * Math.Log(d);
        }

        return num;
    }

    public static double WeightedEntropy(
        this double[] values,
        double[]? weights,
        Func<double, double> pdf)
    {
        var num = 0.0;
        for (var index = 0; index < values.Length; ++index)
        {
            var d = pdf(values[index]) * weights[index];
            num += d * Math.Log(d);
        }

        return num;
    }

    public static double WeightedEntropy(
        this double[] values,
        int[] weights,
        Func<double, double> pdf)
    {
        var num = 0.0;
        for (var index = 0; index < values.Length; ++index)
        {
            var d = pdf(values[index]);
            num += d * Math.Log(d) * weights[index];
        }

        return num;
    }
}