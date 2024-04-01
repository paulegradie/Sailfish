using MathNet.Numerics.LinearAlgebra;
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