using MathNet.Numerics.LinearAlgebra;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

public static class StatsOps
{
    public static IEnumerable<long> EnumerableRange(this long n)
    {
        for (long i = 0; i < n; ++i)
            yield return i;
    }

    public static double WeightedStandardDeviation(this double[] values, double[]? weights)
    {
        return Math.Sqrt(values.WeightedVariance(weights));
    }

    private static double WeightedVariance(this double[] values, double[]? weights)
    {
        return values.WeightedVariance(weights, values.WeightedMean(weights), true);
    }

    private static double WeightedVariance(
        this double[] values,
        double[]? weights,
        double mean,
        bool unbiased,
        WeightType weightType = WeightType.Fraction)
    {
        if (values.Length != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        var sum = 0.0;
        var squareSum = 0.0;
        var weightSum = 0.0;
        for (var index = 0; index < values.Length; ++index)
        {
            var num = values[index] - mean;
            var weight = weights[index];
            sum += weight * (num * num);
            weightSum += weight;
            squareSum += weight * weight;
        }

        return Correct(unbiased, weightType, sum, weightSum, squareSum);
    }

    public static double WeightedStandardDeviation(this double[] values, int[] weights)
    {
        return Math.Sqrt(values.WeightedVariance(weights));
    }

    private static double WeightedVariance(this double[] values, int[] weights)
    {
        return values.WeightedVariance(weights, values.WeightedMean(weights), true);
    }

    private static double WeightedVariance(
        this IReadOnlyList<double> values,
        int[] weights,
        double mean,
        bool unbiased)
    {
        if (values.Count != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        var num1 = 0.0;
        var num2 = 0;
        for (var index = 0; index < values.Count; ++index)
        {
            var num3 = values[index] - mean;
            var weight = weights[index];
            num1 += weight * (num3 * num3);
            num2 += weight;
        }

        return unbiased ? num1 / (num2 - 1.0) : num1 / num2;
    }

    public static T[] Get<T>(this T[] source, int startRow, int endRow)
    {
        startRow = TheIndex(startRow, source.Length);
        endRow = TheEnd(endRow, source.Length);
        var objArray = new T[endRow - startRow];
        for (var index = startRow; index < endRow; ++index)
            objArray[index - startRow] = source[index];
        return objArray;
    }

    private static int TheEnd(int end, int length)
    {
        if (end <= 0)
            end = length + end;
        return end;
    }

    private static int TheIndex(int end, int length)
    {
        if (end < 0)
            end = length + end;
        return end;
    }

    public static double WeightedMean(this double[] values, double[]? weights)
    {
        if (values.Length != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        var num1 = 0.0;
        for (var index = 0; index < values.Length; ++index)
            num1 += weights[index] * values[index];
        var num2 = 0.0;
        for (var index = 0; index < weights.Length; ++index)
            num2 += weights[index];
        return num1 / num2;
    }

    public static double WeightedMean(this double[] values, int[] weights)
    {
        if (values.Length != weights.Length)
            throw new DimensionMismatchException(nameof(weights), "The values and weight vectors must have the same length");
        var num1 = values.Select((t, index) => weights[index] * t).Sum();
        var num2 = Enumerable.Sum(weights);
        return num1 / num2;
    }

    private static double Correct(
        bool unbiased,
        WeightType weightType,
        double sum,
        double weightSum,
        double squareSum)
    {
        if (!unbiased) return sum / weightSum;
        switch (weightType)
        {
            case WeightType.Repetition:
                return sum / (weightSum - squareSum / weightSum);

            case WeightType.Fraction:
                return sum / (weightSum - squareSum / weightSum);

            case WeightType.Automatic:
                return weightSum > 1.0 && 1E-08.IsInteger() ? sum / (weightSum - 1.0) : sum / (weightSum - squareSum / weightSum);

            case WeightType.None:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(weightType), weightType, null);
        }

        return sum / weightSum;
    }

    private static bool IsInteger(this double x, double threshold = 1.49322178960515E-300)
    {
        var num1 = Math.Round(x);
        var num2 = x;
        if (num1 == num2)
            return true;
        var num3 = Math.Abs(num1) * threshold;
        return Math.Abs(num1 - num2) <= num3;
    }

    public static double Mean(this double[] values)
    {
        var num = 0.0;
        for (var index = 0; index < values.Length; ++index)
            num += values[index];
        return num / values.Length;
    }

    public static void Dot(this double[,] a, double[,] b, double[,] result)
    {
        var matrixA = Matrix<double>.Build.DenseOfArray(a);
        var matrixB = Matrix<double>.Build.DenseOfArray(b);

        if (matrixA.ColumnCount != matrixB.RowCount ||
            result.GetLength(0) != matrixA.RowCount ||
            result.GetLength(1) != matrixB.ColumnCount)
            throw new ArgumentException("Matrix dimensions are not aligned for dot product.");

        var resultMatrix = matrixA.Multiply(matrixB);

        for (var i = 0; i < result.GetLength(0); i++)
        for (var j = 0; j < result.GetLength(1); j++)
            result[i, j] = resultMatrix[i, j];
    }

    public static int[] Find<T>(this T[] data, Func<T, bool> func)
    {
        var intList = new List<int>();
        for (var index = 0; index < data.Length; ++index)
            if (func(data[index]))
                intList.Add(index);

        return [.. intList];
    }

    public static T[] Concatenate<T>(this T[] a, params T[] b)
    {
        var objArray = new T[a.Length + b.Length];
        for (var index = 0; index < a.Length; ++index)
            objArray[index] = a[index];
        for (var index = 0; index < b.Length; ++index)
            objArray[index + a.Length] = b[index];
        return objArray;
    }

    public static double StandardDeviation(this double[] values, bool unbiased = true)
    {
        var mean = values.Mean();
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